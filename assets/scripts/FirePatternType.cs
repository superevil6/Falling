public enum FirePatternType
{
	// A single shot toward the base direction.
	Targeted,
	// A fan of bullets spread evenly across an arc, centred on the base direction.
	Spread,
	// Bullets spaced evenly around a full circle.
	Ring,
	// The aim rotates a step each shot, tracing a circle over successive shots
	// (e.g. firing one bullet at a time around a circle).
	Spiral,
}
