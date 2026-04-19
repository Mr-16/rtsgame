shader_type spatial;
// 关键：增加 depth_test_disabled，让文字和标签能正常穿透显示
render_mode unshaded, cull_disabled, depth_test_disabled;

// --- 控制参数 ---
group_uniforms Outline_Settings;
uniform float line_width = 3.0;              // 线条粗细
uniform float depth_threshold = 0.05;        // 深度阈值
uniform float normal_threshold = 0.5;        // 法线阈值
uniform vec3 outline_color : source_color = vec3(0.0, 0.0, 0.0); // 描边颜色

group_uniforms Toon_Settings;
uniform bool use_toon_style = false;         // 默认关闭卡通色块，保持原版画面
uniform float saturation = 1.2;
uniform int color_levels = 5;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear;
uniform sampler2D depth_texture : hint_depth_texture, filter_linear;
uniform sampler2D normal_texture : hint_normal_roughness_texture, filter_linear;

void vertex() {
    POSITION = vec4(VERTEX.xy, 1.0, 1.0);
}

void fragment() {
    vec2 uv = SCREEN_UV;
    vec2 pixel_size = 1.0 / VIEWPORT_SIZE;

    // --- 1. 直接获取屏幕原始颜色 ---
    vec4 screen_col = texture(screen_texture, uv);
    vec3 color = screen_col.rgb;

    // 如果你以后想偶尔开启卡通效果，可以勾选参数，默认是关闭的
    if (use_toon_style) {
        float luma = dot(color, vec3(0.299, 0.587, 0.114));
        color = mix(vec3(luma), color, saturation);
        color = floor(color * float(color_levels)) / float(color_levels);
    }

    // --- 2. 深度边缘检测 ---
    float d = texture(depth_texture, uv).r;
    float du = texture(depth_texture, uv + vec2(0.0, pixel_size.y) * line_width).r;
    float dl = texture(depth_texture, uv + vec2(-pixel_size.x, 0.0) * line_width).r;
    
    float edge_d = abs(d - du) + abs(d - dl);
    float mask_d = step(depth_threshold * 0.1, edge_d);

    // --- 3. 法线边缘检测 ---
    vec3 n = texture(normal_texture, uv).rgb;
    vec3 nu = texture(normal_texture, uv + vec2(0.0, pixel_size.y) * line_width).rgb;
    vec3 nl = texture(normal_texture, uv + vec2(-pixel_size.x, 0.0) * line_width).rgb;
    
    float edge_n = distance(n, nu) + distance(n, nl);
    float mask_n = step(normal_threshold, edge_n);

    // --- 4. 混合最终画面 ---
    float final_mask = clamp(mask_d + mask_n, 0.0, 1.0);
    
    // 只在边缘处覆盖颜色，其他地方完全保留原版屏幕内容
    ALBEDO = mix(color, outline_color, final_mask);
}