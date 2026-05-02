shader_type spatial;

// 基础材质属性
uniform vec4 albedo : source_color = vec4(0.1, 0.2, 0.1, 1.0);
uniform vec4 flash_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float idle_speed = 2.5;
uniform float move_speed = 8.0;
uniform float atk_speed = 5.0; // 稍微加快攻击频率

// 定义 varying 变量用于跨函数传递数据
varying float v_is_hurt;

void vertex() {
    // 1. 获取数据
    float state = INSTANCE_CUSTOM.g;
    float is_hurt = INSTANCE_CUSTOM.b; 
    float instance_offset = INSTANCE_CUSTOM.r * 0.15;
    
    v_is_hurt = is_hurt;
    
    float t = TIME + instance_offset;
    vec3 v = VERTEX;
    
    // 基于高度的缩放因子 (确保模型 Y>=0)
    float h_factor = clamp(v.y, 0.0, 1.0); 

    if (state == 0.0) { 
        // --- Idle: 夸张的呼吸 ---
        float breath = (sin(t * idle_speed) * 0.5 + 0.5) * 0.25;
        v.y += breath * h_factor;
        v.xz *= (1.0 - breath * 0.5 * h_factor);
        
    } else if (state == 1.0) {
        // --- Move: 极具弹性的跳跃 ---
        float jump_sine = sin(t * move_speed);
        float jump_f = max(0.0, jump_sine); 
        
        v.y += jump_f * 0.6 * h_factor;
        float stretch = jump_sine * 0.35;
        v.y *= (1.0 + stretch * h_factor);
        v.xz *= (1.0 - stretch * 0.5 * h_factor);
        v.x += jump_sine * 0.2 * h_factor;
        
    } else if (state == 2.0) {
        // --- Atk: 优化后的重击 (爆发式形变) ---
        float cycle = fract(t * atk_speed / 6.28); 
        
        float punch = 0.0;
        float forward_lean = 0.0;
        
        if (cycle < 0.6) {
            // 1. 蓄力阶段 (0.0 - 0.6): 慢慢压扁，身体后倾
            float prep_t = cycle / 0.6;
            punch = -0.5 * pow(prep_t, 2.0); // 使用平方曲线，积压感更强
            forward_lean = -0.2 * prep_t;   // 轻微后仰蓄力
        } else if (cycle < 0.75) {
            // 2. 爆发阶段 (0.6 - 0.75): 瞬间拉长并前冲
            float burst_t = (cycle - 0.6) / 0.15;
            punch = mix(-0.5, 1.2, burst_t); // 从压扁瞬间变为极度拉伸
            forward_lean = mix(-0.2, 1.0, burst_t); // 猛烈前冲
        } else {
            // 3. 回弹阶段 (0.75 - 1.0): 缓慢恢复
            float recover_t = (cycle - 0.75) / 0.25;
            punch = mix(1.2, 0.0, 1.0 - pow(1.0 - recover_t, 3.0));
            forward_lean = mix(1.0, 0.0, recover_t);
        }

        // 应用形变
        v.y *= (1.0 + punch * h_factor);
        v.xz *= (1.0 - punch * 0.4 * h_factor); // 保持体积守恒
        v.z += forward_lean * 0.8 * h_factor;   // 核心：Z轴的前冲力度
        
    } else if (state == 3.0) {
        // --- Death: 融化 ---
        v.y *= 0.1;
        v.xz *= 1.8;
    }

    // --- Hurt: 强力抖动 ---
    if (is_hurt > 0.5) {
        float shake = sin(TIME * 60.0) * 0.15;
        v.x += shake * h_factor;
    }

    VERTEX = v;
}

void fragment() {
    vec3 final_color = albedo.rgb;
    
   if (v_is_hurt > 0.0) {
        // 使用 sin 或简单的 step 创造闪烁感
        // 注意：0.1秒非常短，可能只够闪 1-2 下
        float flash = step(0.0, sin(TIME * 40.0)); 
        final_color = mix(final_color, flash_color.rgb, flash);
    }
    
    ALBEDO = final_color;
    ROUGHNESS = 0.2;
    SPECULAR = 0.5;
}