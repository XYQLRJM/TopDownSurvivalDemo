using System;

[Serializable]
/// <summary>
/// 表示一个可选择的升级强化选项。
/// </summary>
public class BuffOption
{
    /// <summary>强化影响的属性类型。</summary>
    public string type;
    /// <summary>强化增加的数值。</summary>
    public int value;
    /// <summary>显示在强化按钮上的描述文本。</summary>
    public string description;

    /// <summary>创建一个强化选项。</summary>
    public BuffOption(string type, int value, string description)
    {
        this.type = type;
        this.value = value;
        this.description = description;
    }
}
