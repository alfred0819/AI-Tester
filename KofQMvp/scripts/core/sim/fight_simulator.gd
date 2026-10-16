class_name FightSimulator
extends RefCounted
## Local authoritative 2-fighter sim (Phase 0 movement).

var p1 := FighterBody.new()
var p2 := FighterBody.new()
var tick: int = 0


func setup_default() -> void:
	p1.player_id = 1
	p2.player_id = 2
	p1.reset(Vector2(360, p1.ground_y), 1)
	p2.reset(Vector2(920, p2.ground_y), -1)


func step(i1: FightInput, i2: FightInput, dt: float = FightClock.TICK_DELTA) -> void:
	tick += 1
	p1.tick_movement(i1, dt)
	p2.tick_movement(i2, dt)
	_separate()


func _separate() -> void:
	var min_dist := 56.0
	var dx: float = p2.pos.x - p1.pos.x
	if absf(dx) >= min_dist:
		return
	var push: float = (min_dist - absf(dx)) * 0.5
	var s: float = signf(dx) if dx != 0.0 else 1.0
	p1.pos.x -= push * s
	p2.pos.x += push * s
	p1.pos.x = clampf(p1.pos.x, p1.stage_left, p1.stage_right)
	p2.pos.x = clampf(p2.pos.x, p2.stage_left, p2.stage_right)
