// NOTE: Shader automatically converted and modified for Health Bar
shader_type spatial;
// unshaded 让血条不受光照影响，保证在暗处也清晰
render_mode blend_mix, depth_draw_opaque, cull_disabled, diffuse_burley, specular_schlick_ggx, unshaded;

// --- 新增：血条控制参数 ---
uniform float health_value : hint_range(0.0, 1.0) = 0.8; 
uniform vec4 color_good : source_color = vec4(0.0, 1.0, 0.0, 1.0); 
uniform vec4 color_bad : source_color = vec4(1.0, 0.0, 0.0, 1.0);  
uniform vec4 color_bg : source_color = vec4(0.1, 0.1, 0.1, 0.8);   

// 保留转换生成的 UV 参数
uniform vec3 uv1_scale;
uniform vec3 uv1_offset;

void vertex() {
	UV = UV * uv1_scale.xy + uv1_offset.xy;

	// 这是 Godot 自动生成的广告牌核心代码，不要动它
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
			MAIN_CAM_INV_VIEW_MATRIX[0],
			MAIN_CAM_INV_VIEW_MATRIX[1],
			MAIN_CAM_INV_VIEW_MATRIX[2],
			MODEL_MATRIX[3]);
	MODELVIEW_NORMAL_MATRIX = mat3(MODELVIEW_MATRIX);
}

void fragment() {
	// --- 新增：血条渲染逻辑 ---
	// 计算血条遮罩
	float mask = step(UV.x, health_value);
	
	// 根据血量在好颜色和坏颜色之间插值（比如满血绿，残血红）
	vec3 health_color = mix(color_bad.rgb, color_good.rgb, health_value);
	
	// 混合背景色
	vec3 final_color = mix(color_bg.rgb, health_color, mask);
	
	ALBEDO = final_color;
	// 混合背景透明度
	ALPHA = mix(color_bg.a, 1.0, mask);
}