using UnityEngine;

[RequireComponent (typeof (LineRenderer))]
public class GrappleEffect : MonoBehaviour
{
    	[Tooltip("The Origin of the grapple, Own transform is used if no value set.")]
    	public Transform Origin;
	[Tooltip ("The speed of the entire effect.")]
	public float Speed = 3f;
	[Tooltip ("The speed of the spiral offset (relative to 'Speed').")]
	public float SpiralSpeed = 4f;
	[Tooltip ("The speed that the end of the line moves to the target point (relative to 'Speed')")]
	public float DistanceSpeed = 2f;
	[Tooltip ("A multiplier for the pull of gravity.")]
	public float Gravity = 0.5f;
	[Tooltip ("The number of points to be drawn by the line renderer. \nUpdates every time the effect is triggered.")]
	public int Segments = 100;

	[Header ("Spiral")]
	[Tooltip ("The strength of the spiral.")]
	public Vector2 Magnitude = Vector2.one;
	[Tooltip ("The number of 'Curve' repetitions per world-unit.")]
	public float Frequency = 0.5f;
	[Tooltip ("The amount the horizontal part of the spiral is offset along 'Curve.' \nIf 'Curve' is a sine wave, 0.25 will result in a perfect spiral.")]
	public float HorizontalOffset = 0.25f;

	[Header ("Noise")]
	[Tooltip ("The strength of the noise.")]
	public float Strength = 0.5f;
	[Tooltip ("The scale of the noise samples. \nHigher = smoother.")]
	public float Scale = 0.25f;

	[Header ("Curves")]
	[Tooltip ("The base curve that the points will be offset along. \nA sine wave will make it look like a grapple spiral.")]
	public AnimationCurve Curve = new AnimationCurve ();
	[Tooltip ("The strength of the spiral and noise over time.")]
	public AnimationCurve MagnitudeOverTime = new AnimationCurve ();
	[Tooltip ("The strength of the spiral and noise from the start to current end point within a 0 to 1 range.")]
	public AnimationCurve MagnitudeOverDistance = new AnimationCurve ();
	[Tooltip ("The vertical offset applied in worldspace to the line from the start to current end point within a 0 to 1 range.")]
	public AnimationCurve GravityOverDistance = new AnimationCurve ();
	[Tooltip ("The strength of the gravity over time.")]
	public AnimationCurve GravityOverTime = new AnimationCurve ();

	private float scaledTimeOffset = 0f;
	private float spiralTimeOffset = 0f;
	private float lastGrappleTime = 0f;
	private Vector3 grapplePoint;
	private LineRenderer lineRenderer;

	private void Awake ()
	{
		lineRenderer = GetComponent<LineRenderer> ();
	}

    private void Start()
    {
        lineRenderer.enabled = false;
        if (!Origin)
        {
            Origin = transform;
        }
    }

    public void DoGrapple ()
	{
		RaycastHit hit;
		if (lineRenderer.enabled = Physics.Raycast (Origin.position, Origin.forward, out hit))
			DoGrapple (hit.point);
	}

	public void DoGrapple (Vector3 grapplePoint)
    {
        lineRenderer.enabled = false;

        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            lineRenderer.SetPosition(i, Origin.position);
        }

        this.grapplePoint = grapplePoint;
		scaledTimeOffset = spiralTimeOffset = 0f;
		if (lineRenderer.positionCount != Segments)
			lineRenderer.positionCount = Segments;

		lastGrappleTime = Time.time * 10f;
        lineRenderer.enabled = true;
    }

	// I like this naming convention haha
	public void StopGrapple ()
	{
		lineRenderer.enabled = false;
	}

	public void SetGrapplePoint (Vector3 grapplePoint)
	{
		this.grapplePoint = grapplePoint;
	}

	private void Update ()
	{
		if (!lineRenderer.enabled)
			return;

		var difference = grapplePoint - Origin.position;
		var direction = difference.normalized;
		var distanceMultiplier = Mathf.Clamp01 (scaledTimeOffset * DistanceSpeed);
		var distance = difference.magnitude * distanceMultiplier;

		scaledTimeOffset += Speed * Time.deltaTime;
		if (distanceMultiplier < 1f)
			spiralTimeOffset += Speed * SpiralSpeed * Time.deltaTime;

		for (int i = 0; i < lineRenderer.positionCount; i++)
		{
			var t = (float)i / lineRenderer.positionCount;
			var position = Origin.position;
			var forwardOffset = direction * (t * distance);
			position += forwardOffset;

			var curveSamplePosition = forwardOffset.magnitude * Frequency - spiralTimeOffset;

			var verticalOffset = Origin.up * Curve.Evaluate (curveSamplePosition);
			var horizontalOffset = Origin.right * Curve.Evaluate (curveSamplePosition + HorizontalOffset);

			verticalOffset *= Magnitude.y;
			horizontalOffset *= Magnitude.x;

			var noiseSamplePosition = -t * Scale + scaledTimeOffset + lastGrappleTime;
			verticalOffset += Origin.up * ((Mathf.PerlinNoise (0f, noiseSamplePosition) - 0.5f) * 2f * Strength);
			horizontalOffset += Origin.right * ((Mathf.PerlinNoise (noiseSamplePosition, 0f) - 0.5f) * 2f * Strength);

			var magnitude = MagnitudeOverTime.Evaluate (scaledTimeOffset) * MagnitudeOverDistance.Evaluate (t);
			verticalOffset *= magnitude;
			horizontalOffset *= magnitude;

			position += verticalOffset;
			position += horizontalOffset;

			position += Vector3.up * (GravityOverDistance.Evaluate (t) * GravityOverTime.Evaluate (scaledTimeOffset) * Gravity);

			lineRenderer.SetPosition (i, position);
		}
	}
}
