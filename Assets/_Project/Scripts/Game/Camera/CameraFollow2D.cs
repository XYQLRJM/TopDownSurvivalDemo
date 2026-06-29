using UnityEngine;

/// <summary>
/// 跟随玩家，并保证摄像机视野不会超出地图精灵边界。
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    /// <summary>摄像机平滑追随目标所需时间。</summary>
    [SerializeField] private float smoothTime = 0.12f;

    /// <summary>当前摄像机组件。</summary>
    private Camera cam;
    /// <summary>摄像机要追随的玩家目标。</summary>
    private Transform target;
    /// <summary>地图边界，用来限制镜头不拍到地图外。</summary>
    private Bounds mapBounds;
    /// <summary>SmoothDamp 使用的速度缓存。</summary>
    private Vector3 velocity;

    /// <summary>初始化摄像机追随目标和地图边界。</summary>
    public void Init(Transform followTarget, Bounds bounds)
    {
        cam = GetComponent<Camera>();
        target = followTarget;
        mapBounds = bounds;
        FitCameraInsideMap();
        SnapToTarget();
    }

    /// <summary>切换摄像机追随目标。</summary>
    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
        SnapToTarget();
    }

    /// <summary>在玩家移动后更新摄像机位置。</summary>
    private void LateUpdate()
    {
        if (target == null || cam == null)
            return;

        Vector3 targetPos = new Vector3(target.position.x, target.position.y, transform.position.z);
        Vector3 clampedPos = ClampCameraPosition(targetPos);
        transform.position = Vector3.SmoothDamp(transform.position, clampedPos, ref velocity, smoothTime);
    }

    /// <summary>根据地图大小限制摄像机缩放，避免视野大于地图。</summary>
    private void FitCameraInsideMap()
    {
        if (cam == null || mapBounds.size == Vector3.zero)
            return;

        float maxByHeight = mapBounds.size.y * 0.5f;
        float maxByWidth = mapBounds.size.x / (2f * cam.aspect);
        cam.orthographicSize = Mathf.Min(cam.orthographicSize, maxByHeight, maxByWidth);
    }

    /// <summary>立即把摄像机移动到目标附近，不做平滑过渡。</summary>
    private void SnapToTarget()
    {
        if (target == null || cam == null)
            return;

        transform.position = ClampCameraPosition(new Vector3(target.position.x, target.position.y, transform.position.z));
    }

    /// <summary>把摄像机中心点限制在地图可见范围内。</summary>
    private Vector3 ClampCameraPosition(Vector3 pos)
    {
        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * cam.aspect;

        if (mapBounds.size.x <= horzExtent * 2f)
            pos.x = mapBounds.center.x;
        else
            pos.x = Mathf.Clamp(pos.x, mapBounds.min.x + horzExtent, mapBounds.max.x - horzExtent);

        if (mapBounds.size.y <= vertExtent * 2f)
            pos.y = mapBounds.center.y;
        else
            pos.y = Mathf.Clamp(pos.y, mapBounds.min.y + vertExtent, mapBounds.max.y - vertExtent);

        return pos;
    }
}
