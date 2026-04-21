shader_type spatial;
render_mode unshaded, blend_add, cull_disabled;

group_uniforms Color;
uniform vec3 core_color : source_color = vec3(1.0, 0.9, 0.5); // 核心颜色（亮黄/白）
uniform vec3 edge_color : source_color = vec3(1.0, 0.2, 0.0); // 边缘颜色（深红/橙）
uniform float emission_energy = 3.0;

group_uniforms Animation;
uniform float speed = 2.0;
uniform float ray_intensity = 1.5;
uniform float noise_scale = 1.5;

group_uniforms Shape;
uniform sampler2D noise_tex; // 请分配一个噪点图 (FastNoiseLite)
uniform sampler2D flare_texture;

group_uniforms Alpha;
uniform float expansion : hint_range(0.0, 1.0) = 0.1;
uniform bool proximity_fade = true;
uniform float proximity_fade_distance = 0.5;

uniform sampler2D depth_texture : hint_depth_texture, repeat_disable, filter_nearest;

varying flat int id;
varying vec3 world_pos;

// 简单的旋转函数
vec2 rotate(vec2 uv, float amount) {
    float s = sin(amount);
    float c = cos(amount);
    return mat2(vec2(c, -s), vec2(s, c)) * uv;
}

void vertex() {
    id = INSTANCE_ID;
    // 基础公告板处理
    MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
        MAIN_CAM_INV_VIEW_MATRIX[0],
        MAIN_CAM_INV_VIEW_MATRIX[1],
        MAIN_CAM_INV_VIEW_MATRIX[2],
        MODEL_MATRIX[3]);
    MODELVIEW_NORMAL_MATRIX = mat3(MODELVIEW_MATRIX);
}

void fragment() {
    vec2 base_uv = UV;
    vec2 centered_uv = UV - 0.5;
    
    // 1. 极坐标转换 (用于制作放射状效果)
    float dist = length(centered_uv);
    float angle = atan(centered_uv.y, centered_uv.x);
    
    // 2. 动态噪点扭曲
    // 使用时间流逝来让火球产生搅动感
    float time = TIME * speed + float(id) * 12.45;
    vec2 noise_uv = vec2(angle * noise_scale, dist - time * 0.5);
    float noise_val = texture(noise_tex, noise_uv).r;
    
    // 3. 混合基础纹理
    float flare = texture(flare_texture, base_uv).r;
    
    // 4. 计算火球边缘 - 让它看起来像在燃烧
    // 这里的逻辑是：距离中心越远，受噪点的影响越大
    float fire_mask = smoothstep(0.5, 0.1, dist + noise_val * 0.2);
    
    // 5. 放射光芒 (Streaks)
    float rays = pow(cos(angle * 6.0 + time) * 0.5 + 0.5, 10.0) * ray_intensity;
    rays *= smoothstep(0.5, 0.2, dist); // 只在中心附近产生光芒
    
    // 6. 最终颜色合成
    // 靠近中心使用核心色，边缘使用火红色
    float color_mix = smoothstep(0.1, 0.4, fire_mask);
    vec3 final_color = mix(edge_color, core_color, fire_mask);
    
    // 叠加光芒和初始贴图
    float alpha_mask = fire_mask + rays * 0.5;
    alpha_mask *= flare; // 结合传入的贴图形状
    
    ALBEDO = final_color * emission_energy * COLOR.rgb;
    ALPHA = clamp(alpha_mask * COLOR.a, 0.0, 1.0);

    // Proximity Fade (软化与地面接触的边缘)
    if(proximity_fade){
        float proximity_depth_tex = textureLod(depth_texture, SCREEN_UV, 0.0).r;
        vec4 ndc = vec4(SCREEN_UV * 2.0 - 1.0, proximity_depth_tex, 1.0);
        vec4 proximity_view_pos = INV_PROJECTION_MATRIX * ndc;
        proximity_view_pos.xyz /= proximity_view_pos.w;
        ALPHA *= clamp(1.0 - smoothstep(proximity_view_pos.z + proximity_fade_distance, proximity_view_pos.z, VERTEX.z), 0.0, 1.0);
    }
}