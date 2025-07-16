using System;
using System.Collections.Generic;
using UnityEngine;

public class Tween_RC
{
    protected float duration;
    protected float elapsed;
    protected Action<float> onTweenUpdate;
    protected Action onTweenComplete;
    protected Func<float, float> easeFunction;
    public bool IsComplete { get; private set; }

    public Tween_RC(float duration, Action<float> onTweenUpdate, Action onTweenComplete)
    {
        this.duration = duration;
        this.onTweenUpdate = onTweenUpdate;
        this.onTweenComplete = onTweenComplete;
        this.easeFunction = linear;

        this.elapsed = 0;
        this.IsComplete = false;
    }

    public virtual void Update(float deltaTime)
    {
        elapsed += deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        t = easeFunction(t);

        onTweenUpdate?.Invoke(t);

        if (elapsed >= duration)
        {
            IsComplete = true;
            onTweenComplete?.Invoke();
        }
    }

    public Tween_RC SetEaseFunction(Func<float, float> easeFunction)
    {
        this.easeFunction = easeFunction;
        return this;
    }

    // 缓动函数 Ease Functions
    public static float linear(float t) => t;
    public static float easeInQuad(float t) => t * t;
    public static float easeOutQuad(float t) => 1 - (1 - t) * (1 - t);

    public static float easeInAndOutQuad(float t)
    {
        if (t < 0.5f)
        {
            return 2 * t * t;
        }
        else
        {
            return 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
        }
    }
}

public class TweenManager : Singleton<TweenManager>
{
    List<Tween_RC> tweenList = new List<Tween_RC>();

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }

    void Update()
    {
        for (int i = tweenList.Count - 1; i >= 0; i--)
        {
            var tween = tweenList[i];
            tween.Update(Time.deltaTime);
            if (tween.IsComplete)
            {
                tweenList.RemoveAt(i);
            }
        }
    }

    public void AddTween(Tween_RC tween)
    {
        tweenList.Add(tween);
    }
}

public static class TweenExtensions_RC
{
    public static Tween_RC To(this float from, float to, float duration, Action<float> onTweenUpdate,
        Action onTweenComplete = null)
    {
        var tween = new Tween_RC(
            duration,
            (float t) => onTweenUpdate(Mathf.Lerp(from, to, t)),
            onTweenComplete
        );

        TweenManager.Instance.AddTween(tween);
        return tween;
    }

    public static Tween_RC To(this Vector3 from, Vector3 to, float duration, Action<Vector3> onTweenUpdate,
        Action onTweenComplete = null)
    {
        var tween = new Tween_RC(
            duration,
            (float t) => onTweenUpdate(Vector3.Lerp(from, to, t)),
            onTweenComplete
        );

        TweenManager.Instance.AddTween(tween);
        return tween;
    }

    public static Tween_RC DoMove(this Transform transform, Vector3 endValue, float duration)
    {
        Vector3 startPos = transform.position;
        return startPos.To(endValue, duration, (Vector3 pos) => transform.position = pos);
    }

    public static Tween_RC DoScale(this Transform transform, Vector3 endValue, float duration)
    {
        Vector3 startScale = transform.localScale;
        return startScale.To(endValue, duration, (Vector3 scale) => transform.localScale = scale);
    }
}