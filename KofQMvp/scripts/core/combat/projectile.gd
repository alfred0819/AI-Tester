class_name FightProjectile
extends RefCounted

var owner_id: int = 0
var pos := Vector2.ZERO
var vel := Vector2.ZERO
var life: int = 0
var w: float = 32.0
var h: float = 24.0
var move: MoveDef
var hit_targets: Dictionary = {}
var dead: bool = false


func box() -> Aabb2:
	return Aabb2.new(pos.x - w * 0.5, pos.y - h * 0.5, w, h)


func tick(dt: float) -> void:
	if dead:
		return
	pos += vel * dt
	life -= 1
	if life <= 0:
		dead = true
