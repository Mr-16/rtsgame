shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled, unshaded;

uniform float health_value : hint_range(0.0, 1.0) = 0.8;
uniform float buffer_value : hint_range(0.0, 1.0) = 0.9; 

uniform vec4 color_good : source_color = vec4(0.2, 1.0, 0.4, 1.0);
uniform vec4 color_bad : source_color = vec4(1.0, 0.1, 0.1, 1.0);
uniform vec4 color_buffer : source_color = vec4(1.0, 1.0, 1.0, 0.5); 
uniform vec4 color_bg : source_color = vec4(0.05, 0.05, 0.05, 0.7);

// --- 关键修正：尺寸适配 ---
uniform vec2 quad_size = vec2(1.0, 0.2);      // 填入你 QuadMesh 的实际大小
uniform vec4 color_border : source_color = vec4(0, 0, 0, 1.0);
uniform vec2 border_thickness = vec2(0.02, 0.02); // 现在的厚度是世界单位，更直观
uniform float corner_radius : hint_range(0.0, 1.0) = 0.05; // 圆角半径

uniform vec2 smoothness = vec2(0.01, 0.01); 
uniform float pulse_speed : hint_range(0.0, 10.0) = 5.0; 

// 修正后的 SDF 函数：支持非等比缩放
float sd_rounded_box(vec2 p, vec2 b, float r) {
    vec2 q = abs(p) - b + r;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
}

void vertex() {
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
			MAIN_CAM_INV_VIEW_MATRIX[0],
			MAIN_CAM_INV_VIEW_MATRIX[1],
			MAIN_CAM_INV_VIEW_MATRIX[2],
			MODEL_MATRIX[3]);
	MODELVIEW_NORMAL_MATRIX = mat3(MODELVIEW_MATRIX);
}

void fragment() {
	// 使用实际尺寸修正 UV 坐标，解决拉伸问题
	vec2 p = (UV - 0.5) * quad_size;
	vec2 half_size = quad_size * 0.5;
	
	// --- 1. 颜色与动画 ---
	vec3 base_health_rgb = mix(color_bad.rgb, color_good.rgb, health_value);
	float pulse = (health_value < 0.25) ? (1.0 + 0.4 * sin(TIME * pulse_speed)) : 1.0;
	
	// --- 2. 形状定义 (基于物理尺寸) ---
	float outer_sdf = sd_rounded_box(p, half_size, corner_radius);
	
	// 内部尺寸 = 总尺寸 - 2倍边框厚度
	vec2 inner_half_size = half_size - border_thickness;
	// 内部圆角随之缩小
	float inner_radius = max(0.0, corner_radius - min(border_thickness.x, border_thickness.y));
	float inner_sdf = sd_rounded_box(p, inner_half_size, inner_radius);

	float bar_mask = smoothstep(smoothness.y, -smoothness.y, outer_sdf);   
	float inset_mask = smoothstep(smoothness.y, -smoothness.y, inner_sdf); 
	
	// --- 3. 进度映射 ---
	// 在物理尺寸空间计算进度裁剪
	float current_health_mask = 1.0 - smoothstep(health_value - smoothness.x, health_value, (p.x / (inner_half_size.x * 2.0)) + 0.5);
	float buffer_mask = 1.0 - smoothstep(buffer_value - smoothness.x, buffer_value, (p.x / (inner_half_size.x * 2.0)) + 0.5);
	
	// --- 4. 颜色合成 ---
	vec3 final_rgb = color_border.rgb;
	float final_alpha = bar_mask * color_border.a;
	
	final_rgb = mix(final_rgb, color_bg.rgb, inset_mask);
	final_alpha = mix(final_alpha, color_bg.a * bar_mask, inset_mask);
	
	if (buffer_value > health_value) {
		float b_mask = buffer_mask * inset_mask;
		final_rgb = mix(final_rgb, color_buffer.rgb, b_mask);
		final_alpha = max(final_alpha, b_mask * color_buffer.a);
	}
	
	float h_mask = current_health_mask * inset_mask;
	final_rgb = mix(final_rgb, base_health_rgb * pulse, h_mask);
	final_alpha = max(final_alpha, h_mask * color_good.a);

	ALBEDO = final_rgb;
	ALPHA = final_alpha;
}