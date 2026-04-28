shader_type spatial;

// --- 这里定义对外接口 (Uniforms) ---
uniform vec4 grid_color : source_color = vec4(0.1, 0.8, 0.2, 1.0); // 颜色和透明度
uniform float grid_size = 1.0;    // 网格大小
uniform float line_width = 0.1;   // 线条粗细 (0.0 到 0.5 之间)
uniform float antialias = 0.02;   // 抗锯齿过渡，数值越小越锐利

varying vec3 world_position;

void vertex() {
    // 传递世界坐标
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
    // 1. 使用我们定义的 grid_size
    vec2 pos = world_position.xz / grid_size;
    
    // 2. 计算网格线条
    // fwidth 用于根据相机距离自动调整缩放，防止远处的线变糊或闪烁
    vec2 grid = abs(fract(pos - 0.5) - 0.5) / fwidth(pos);
    float line = min(grid.x, grid.y);
    
    // 3. 使用我们定义的 line_width 和 antialias
    // smoothstep 让线条边缘平滑，不至于有锯齿
    float mask = 1.0 - smoothstep(line_width, line_width + antialias, line);
    
    // 4. 应用颜色和透明度
    ALBEDO = grid_color.rgb;
    ALPHA = mask * grid_color.a; // 混合参数里的 alpha
}