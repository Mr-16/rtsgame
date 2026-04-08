shader_type spatial;
// unshaded: 不受光照影响（发光感）
// blend_mix: 标准透明混合
// depth_draw_never: 防止遮挡其他透明物体
render_mode unshaded, blend_mix, depth_draw_never, cull_back;

uniform vec4 main_color : source_color = vec4(0.0, 1.0, 0.5, 1.0);
uniform float inner_radius : hint_range(0.0, 0.5) = 0.4;
uniform float outer_radius : hint_range(0.0, 0.5) = 0.45;
uniform float thickness_softness : hint_range(0.0, 0.1) = 0.01; // 边缘平滑度

// 虚线设置
uniform float segment_count : hint_range(0.0, 50.0) = 0.0; // 0 为实线
uniform float segment_spacing : hint_range(0.0, 1.0) = 0.5; // 缺口大小
uniform float rotation_speed : hint_range(-10.0, 10.0) = 0.0;

// 动态效果
uniform float pulse_speed : hint_range(0.0, 10.0) = 0.0;
uniform float pulse_amplitude : hint_range(0.0, 0.2) = 0.05;

void fragment() {
    // 1. 建立中心坐标系
    vec2 uv = UV - 0.5;
    float dist = length(uv);
    
    // 2. 计算脉冲缩放 (随时间变化半径)
    float pulse = sin(TIME * pulse_speed) * pulse_amplitude;
    float current_inner = inner_radius + pulse;
    float current_outer = outer_radius + pulse;
    
    // 3. 绘制圆环 (使用 smoothstep 保证无论放大多少倍都没有锯齿)
    float ring = smoothstep(current_inner - thickness_softness, current_inner, dist) * (1.0 - smoothstep(current_outer, current_outer + thickness_softness, dist));
    
    // 4. 虚线逻辑
    if (segment_count > 0.0) {
        // 计算当前像素相对于中心的角度
        float angle = atan(uv.y, uv.x) + TIME * rotation_speed;
        // 使用 sin 函数产生周期性的 0 和 1
        float dash = sin(angle * segment_count);
        // 通过 step 调整虚线的间距
        ring *= step(1.0 - segment_spacing, (dash + 1.0) * 0.5);
    }
    
    // 输出结果
    ALBEDO = main_color.rgb;
    ALPHA = ring * main_color.a;
}