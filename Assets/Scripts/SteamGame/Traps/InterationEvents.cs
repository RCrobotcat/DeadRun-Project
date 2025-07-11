using System;

public static class InteractionEvents
{
    // 悬停
    public static Action<InteractiveObject> OnMouseHover;

    // 离开
    public static Action<InteractiveObject> OnMouseExit;

    // 点击
    public static Action<InteractiveObject> OnMouseClick;
}