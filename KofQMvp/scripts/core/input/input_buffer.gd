class_name InputBuffer
extends RefCounted
## Ring buffer of recent FightInput frames for command detection.

var _frames: Array[FightInput] = []
var capacity: int = 15


func push(sample: FightInput) -> void:
	_frames.push_back(sample)
	while _frames.size() > capacity:
		_frames.pop_front()


func latest() -> FightInput:
	if _frames.is_empty():
		return FightInput.new()
	return _frames[_frames.size() - 1]


func history() -> Array[FightInput]:
	return _frames


## Very small helper: returns true if dir sequence appears in order within window.
func match_dirs(sequence: Array[int], window: int = 8) -> bool:
	if sequence.is_empty() or _frames.is_empty():
		return false
	var start: int = maxi(0, _frames.size() - window)
	var si := 0
	for i in range(start, _frames.size()):
		if _frames[i].dir == sequence[si]:
			si += 1
			if si >= sequence.size():
				return true
	return false
