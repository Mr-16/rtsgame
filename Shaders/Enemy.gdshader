shader_type spatial;

// 定义 bridge 变量，用于将数据从 vertex 传到 fragment
varying vec4 v_custom_data;

void vertex() {
    v_custom_data = INSTANCE_CUSTOM;
    
    float individual_offset = v_custom_data.r * 100.0;
    // 假设我们在 C# 传入了有效方向，计算速度
    float move_speed = length(v_custom_data.gb); 
    
    // 只有在有速度时才表现动感
    
        // 1. 史莱姆拉伸动画 (上下跳动 + 左右缩放)
        // 使用 sin 的绝对值让它看起来是在地上“弹”
        float bounce = abs(sin(TIME * 10.0 + individual_offset));
        
        // 当向上跳(bounce高)时，Y轴拉长，XZ轴缩小（保体积挤压感）
        float stretch = 1.0 + bounce * 0.3;
        float squash = 1.0 - bounce * 0.15;
        
        VERTEX.y *= stretch;
        VERTEX.xz *= squash;

        // 2. 呼吸感倾斜 (朝向运动方向的前后晃动)
        // 因为我们在 C# 已经旋转了 Basis 使得模型正面朝向前进方向
        // 所以在局部坐标系下，我们只需要让它在局部 Z 轴上晃动即可
        if (VERTEX.y > 0.1) {
            float lean = sin(TIME * 10.0 + individual_offset) * 0.2;
            VERTEX.z += lean * VERTEX.y; 
        
    }
}

void fragment() {
    // 基础颜色（深蓝色）
    vec3 base_color = vec3(0.3,0.2, 0.1);
    
    // 使用 R 通道做一点色相偏移，数值不要太大
    float hue_shift = sin(v_custom_data.r * 20.0) * 0.1;
    
    // 加上 clamp 保证颜色分量不会超过 1.0 (变白) 或小于 0.0
    ALBEDO = clamp(base_color + hue_shift, 0.0, 1.0);
    
    // 增加粗糙度，防止环境光反射过强导致的发白
    ROUGHNESS = 0.7;
    METALLIC = 0.0;

    // 受击变红逻辑
    if (v_custom_data.a > 0.5) {
        ALBEDO = vec3(1.0, 0.0, 0.0);
    }
}