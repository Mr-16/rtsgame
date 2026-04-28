shader_type spatial;
// 增加 depth_test_disabled，让效果在最顶层显示
render_mode unshaded, cull_disabled, depth_test_disabled;

// --- 控制参数 ---
group_uniforms Outline_Settings;
uniform float line_width = 2.0;               // 线条粗细
uniform float depth_threshold = 0.02;        // 深度阈值（需要根据场景精细调节）
uniform vec3 outline_color : source_color = vec3(0.0, 0.0, 0.0); // 描边颜色

group_uniforms Toon_Settings;
uniform bool use_toon_style = false;
uniform float saturation = 1.2;
uniform int color_levels = 5;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear;
uniform sampler2D depth_texture : hint_depth_texture, filter_linear;

// 注意：移除了 normal_texture 采样器

void vertex() {
    POSITION = vec4(VERTEX.xy, 1.0, 1.0);
}

void fragment() {
    vec2 uv = SCREEN_UV;
    vec2 pixel_size = 1.0 / VIEWPORT_SIZE;

    // --- 1. 获取屏幕原始颜色 ---
    vec4 screen_col = texture(screen_texture, uv);
    vec3 color = screen_col.rgb;

    if (use_toon_style) {
        float luma = dot(color, vec3(0.299, 0.587, 0.114));
        color = mix(vec3(luma), color, saturation);
        color = floor(color * float(color_levels)) / float(color_levels);
    }

    // --- 2. 深度边缘检测 (Roberts Cross 算子简化版) ---
    // 获取周围像素的深度
    float d = texture(depth_texture, uv).r;
    float du = texture(depth_texture, uv + vec2(0.0, pixel_size.y) * line_width).r;
    float dl = texture(depth_texture, uv + vec2(-pixel_size.x, 0.0) * line_width).r;
    
    // 计算深度差异
    float edge_d = abs(d - du) + abs(d - dl);
    
    // 使用 step 产生硬边
    // 提示：在非 Forward+ 下，深度值可能需要更精细的阈值
    float mask = step(depth_threshold * 0.01, edge_d);

    // --- 3. 混合最终画面 ---
    // 仅使用深度遮罩
    ALBEDO = mix(color, outline_color, mask);
}