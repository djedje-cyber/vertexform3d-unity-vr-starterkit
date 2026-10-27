using UnityEngine;

[DisallowMultipleComponent]
public class PlayAreaLimiter : MonoBehaviour
{
    [Tooltip("World-space centre of the allowed area")]
    [SerializeField] Vector3 center = Vector3.zero;
    [Tooltip("World-space size of the allowed area (Y ignored if clampHeight is off)")]
    [SerializeField] Vector3 size = new Vector3(10f, 3f, 10f);
    [SerializeField] bool clampHeight = false;
    [Tooltip("Leaving the area by more than this in one frame is treated as a teleport and cancelled")]
    [SerializeField] float teleportThreshold = 0.5f;

    static readonly string[] RigTypeNames = { "XROrigin", "XRRig", "OVRCameraRig" };

    Transform rig;
    CharacterController cc;
    Vector3 lastInside;

    Bounds Area => new Bounds(center, size);

    void Start()
    {
        rig = FindRig(transform);
        if (rig == transform)
        {
            Debug.LogError($"{nameof(PlayAreaLimiter)}: '{name}' has no parent rig to move.", this);
            enabled = false;
            return;
        }
        cc = rig.GetComponent<CharacterController>();
        lastInside = Clamp(transform.position);
    }

    void LateUpdate()
    {
        Vector3 head = transform.position;
        Vector3 clamped = Clamp(head);
        Vector3 outside = clamped - head;

        if (outside.sqrMagnitude < 1e-10f)
        {
            lastInside = head;
            return;
        }

        // Far outside in one frame: a teleport, so cancel it. Otherwise: walking into the edge, so clamp.
        Vector3 target = outside.magnitude > teleportThreshold ? lastInside : clamped;
        MoveRigDirect(target - head);
        lastInside = target;
    }

    /// Is this head position inside the allowed area? Teleport scripts can check this first.
    public bool IsInside(Vector3 headPosition) => (Clamp(headPosition) - headPosition).sqrMagnitude < 1e-10f;

    /// Would teleporting the rig here put the head outside the area?
    public bool IsRigDestinationOutside(Vector3 rigDestination) =>
        !IsInside(rigDestination + (transform.position - rig.position));

    Vector3 Clamp(Vector3 p)
    {
        var b = Area;
        return new Vector3(
            Mathf.Clamp(p.x, b.min.x, b.max.x),
            clampHeight ? Mathf.Clamp(p.y, b.min.y, b.max.y) : p.y,
            Mathf.Clamp(p.z, b.min.z, b.max.z));
    }

    void MoveRigDirect(Vector3 offset)
    {
        bool ccWasEnabled = cc != null && cc.enabled;
        if (ccWasEnabled) cc.enabled = false;
        rig.position += offset;
        if (ccWasEnabled) cc.enabled = true;
    }

    Transform FindRig(Transform h)
    {
        for (var t = h.parent; t != null; t = t.parent)
            foreach (var mb in t.GetComponents<MonoBehaviour>())
                if (mb != null && System.Array.IndexOf(RigTypeNames, mb.GetType().Name) >= 0)
                    return t;
        return h.parent != null ? h.root : h;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.8f);
        Gizmos.DrawWireCube(center, size);
    }
}