shader_type spatial;
render_mode unshaded; // 不受光照影响，让红色更亮

instance uniform vec3 flash_color : source_color = vec3(1.0, 0.0, 0.0);
instance uniform float flash_modifier : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    ALBEDO = flash_color;
    // 关键：利用 flash_modifier 控制透明度
    ALPHA = flash_modifier; 
}