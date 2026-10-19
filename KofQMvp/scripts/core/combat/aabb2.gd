class_name Aabb2
extends RefCounted
## Axis-aligned box in fight space (x,y = bottom-center relative or world).

var x: float = 0.0
var y: float = 0.0
var w: float = 0.0
var h: float = 0.0


func _init(px: float = 0.0, py: float = 0.0, pw: float = 0.0, ph: float = 0.0) -> void:
	x = px
	y = py
	w = pw
	h = ph


func overlaps(other: Aabb2) -> bool:
	return x < other.x + other.w and x + w > other.x and y < other.y + other.h and y + h > other.y


func flipped(facing: int) -> Aabb2:
	## facing: 1 = right, -1 = left. Flips x about origin for relative boxes.
	if facing >= 0:
		return Aabb2.new(x, y, w, h)
	return Aabb2.new(-(x + w), y, w, h)
