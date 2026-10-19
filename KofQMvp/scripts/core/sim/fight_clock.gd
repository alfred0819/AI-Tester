class_name FightClock
extends RefCounted
## Fixed 60 Hz fight tick helpers.

const TICK_HZ := 60
const TICK_DELTA := 1.0 / float(TICK_HZ)


static func frames_to_seconds(frames: int) -> float:
	return float(frames) * TICK_DELTA


static func seconds_to_frames(seconds: float) -> int:
	return int(round(seconds * float(TICK_HZ)))
