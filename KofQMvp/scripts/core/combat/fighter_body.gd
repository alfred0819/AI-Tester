class_name FighterBody
extends RefCounted
## Minimal Phase-0 logical fighter (movement only).

enum State { IDLE, WALK, JUMP, ATTACK, BLOCK, HITSTUN, KO }

var player_id: int = 0
var pos := Vector2.ZERO
var vel := Vector2.ZERO
var facing: int = 1
var state: State = State.IDLE
var hp: int = 1000
var max_hp: int = 1000
var on_ground: bool = true

var walk_speed: float = 220.0
var jump_speed: float = 520.0
var gravity: float = 1600.0
var ground_y: float = 560.0
var stage_left: float = 80.0
var stage_right: float = 1200.0

var hurt := Aabb2.new(-28, -120, 56, 120)


func reset(at: Vector2, face: int) -> void:
	pos = at
	vel = Vector2.ZERO
	facing = face
	state = State.IDLE
	hp = max_hp
	on_ground = true


func tick_movement(inp: FightInput, dt: float) -> void:
	if state == State.KO or state == State.HITSTUN:
		_integrate(dt)
		return

	var axis := 0
	if inp.dir in [1, 4, 7]:
		axis = -1
	elif inp.dir in [3, 6, 9]:
		axis = 1

	if on_ground:
		if inp.dir in [7, 8, 9]:
			vel.y = -jump_speed
			on_ground = false
			state = State.JUMP
		elif axis != 0:
			vel.x = float(axis) * walk_speed
			state = State.WALK
			facing = axis
		else:
			vel.x = 0.0
			state = State.IDLE
	else:
		vel.x = float(axis) * walk_speed * 0.85
		if axis != 0:
			facing = axis

	_integrate(dt)


func _integrate(dt: float) -> void:
	if not on_ground:
		vel.y += gravity * dt
	pos += vel * dt
	if pos.y >= ground_y:
		pos.y = ground_y
		vel.y = 0.0
		on_ground = true
		if state == State.JUMP:
			state = State.IDLE
	pos.x = clampf(pos.x, stage_left, stage_right)


func world_hurtbox() -> Aabb2:
	var local := hurt.flipped(facing)
	return Aabb2.new(pos.x + local.x, pos.y + local.y, local.w, local.h)
