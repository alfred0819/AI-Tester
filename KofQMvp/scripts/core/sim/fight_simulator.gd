class_name FightSimulator
extends RefCounted
## Local authoritative fight sim with teams, meter, projectiles.

signal fighter_ko(side: int)
signal match_over(winner_side: int)
signal hit_connected(attacker_side: int, blocked: bool, is_super: bool)

var db := CharacterDB.new()
var balance: Dictionary = {}

var p1 := FighterBody.new()
var p2 := FighterBody.new()
var projectiles: Array = []

var team1: Array[String] = ["C01", "C02", "C03"]
var team2: Array[String] = ["C04", "C03", "C02"]
var idx1: int = 0
var idx2: int = 0
var meter1: float = 0.0
var meter2: float = 0.0
var meter_max: float = 300.0

var tick: int = 0
var frozen: bool = false
var winner_side: int = 0  # 0 none, 1 p1, 2 p2
var swap_cooldown: int = 0

# Training flags
var training: bool = false
var lock_hp: bool = false
var lock_meter: bool = false
var dummy_block: bool = false  # stand block
var dummy_crouch_block: bool = false
var show_boxes: bool = true

var last_event: String = ""


func setup(t1: Array, t2: Array, is_training: bool = false) -> void:
	db.load_all()
	balance = db.balance
	meter_max = float(balance.get("meter_max", 300))
	team1 = []
	team2 = []
	for id in t1:
		team1.append(str(id))
	for id in t2:
		team2.append(str(id))
	idx1 = 0
	idx2 = 0
	meter1 = meter_max if is_training else 0.0
	meter2 = meter_max if is_training else 0.0
	training = is_training
	lock_hp = is_training
	lock_meter = is_training
	frozen = false
	winner_side = 0
	tick = 0
	projectiles.clear()
	_bind_meters()
	_spawn_side(1, true)
	_spawn_side(2, true)


func _bind_meters() -> void:
	p1.player_id = 1
	p2.player_id = 2
	p1.meter_ref = Callable(self, "_meter_api").bind(1)
	p2.meter_ref = Callable(self, "_meter_api").bind(2)


func _meter_api(op: String, amount: float = 0.0, side: int = 1) -> Variant:
	# Callable.bind puts bound args at the end when using call from FighterBody
	return null


func _meter_get(side: int) -> float:
	return meter1 if side == 1 else meter2


func _meter_add(side: int, amount: float) -> void:
	if lock_meter:
		return
	if side == 1:
		meter1 = minf(meter_max, meter1 + amount)
	else:
		meter2 = minf(meter_max, meter2 + amount)


func _meter_spend(side: int, amount: float) -> bool:
	if lock_meter:
		return true
	if side == 1:
		if meter1 < amount:
			return false
		meter1 -= amount
		return true
	if meter2 < amount:
		return false
	meter2 -= amount
	return true


func _make_meter_callable(side: int) -> Callable:
	return func(op: String, amount: float = 0.0) -> Variant:
		match op:
			"get":
				return _meter_get(side)
			"add":
				_meter_add(side, amount)
				return true
			"spend":
				return _meter_spend(side, amount)
		return null


func _spawn_side(side: int, first: bool) -> void:
	var intro := 0 if first else int(balance.get("intro_invuln_frames", 40))
	if side == 1:
		var cdef: CharacterDef = db.get_char(team1[idx1])
		p1.apply_character(cdef, balance)
		p1.meter_ref = _make_meter_callable(1)
		p1.reset(Vector2(360, float(balance.get("ground_y", 560))), 1, intro)
	else:
		var cdef2: CharacterDef = db.get_char(team2[idx2])
		p2.apply_character(cdef2, balance)
		p2.meter_ref = _make_meter_callable(2)
		p2.reset(Vector2(920, float(balance.get("ground_y", 560))), -1, intro)


func reset_training_positions() -> void:
	projectiles.clear()
	frozen = false
	winner_side = 0
	_spawn_side(1, true)
	_spawn_side(2, true)
	if lock_meter:
		meter1 = meter_max
		meter2 = meter_max


func step(i1: FightInput, i2: FightInput, dt: float = FightClock.TICK_DELTA) -> void:
	if frozen:
		return
	tick += 1
	if swap_cooldown > 0:
		swap_cooldown -= 1

	if training and dummy_block:
		i2 = _force_block_input(i2, dummy_crouch_block)

	# Face each other
	if p1.pos.x <= p2.pos.x:
		p1.facing = 1
		p2.facing = -1
	else:
		p1.facing = -1
		p2.facing = 1

	p1.tick(i1, dt, balance)
	p2.tick(i2, dt, balance)
	_spawn_pending_projectiles(p1)
	_spawn_pending_projectiles(p2)
	_tick_projectiles(dt)
	_separate()
	_resolve_strikes(p1, p2, i2)
	_resolve_strikes(p2, p1, i1)
	_resolve_projectile_hits()
	_training_locks()
	_check_ko()


func _force_block_input(inp: FightInput, crouch: bool) -> FightInput:
	var out := FightInput.new()
	# Hold away from p1: p2 faces -1 when on right, so back is right (6)
	if p2.facing < 0:
		out.dir = 3 if crouch else 6
	else:
		out.dir = 1 if crouch else 4
	out.buttons = inp.buttons
	out.pressed = inp.pressed
	out.released = inp.released
	return out


func _spawn_pending_projectiles(f: FighterBody) -> void:
	if f.pending_projectile.is_empty():
		return
	var info: Dictionary = f.pending_projectile.duplicate()
	f.pending_projectile.clear()
	var move: MoveDef = info.get("move", null)
	if move == null:
		return
	var proj := FightProjectile.new()
	proj.owner_id = f.player_id
	proj.move = move
	proj.w = float(info.get("w", 32))
	proj.h = float(info.get("h", 24))
	proj.life = int(info.get("life", 60))
	var yoff := float(info.get("y", -70))
	proj.pos = Vector2(f.pos.x + float(f.facing) * 40.0, f.pos.y + yoff)
	proj.vel = Vector2(float(f.facing) * float(info.get("speed", 400)), 0)
	projectiles.append(proj)


func _tick_projectiles(dt: float) -> void:
	for p in projectiles:
		p.tick(dt)
	projectiles = projectiles.filter(func(p): return not p.dead)


func _separate() -> void:
	var min_dist := float(balance.get("min_fighter_gap", 56))
	var dx: float = p2.pos.x - p1.pos.x
	if absf(dx) >= min_dist:
		return
	var push: float = (min_dist - absf(dx)) * 0.5
	var s: float = signf(dx) if dx != 0.0 else 1.0
	p1.pos.x -= push * s
	p2.pos.x += push * s
	p1.pos.x = clampf(p1.pos.x, p1.stage_left, p1.stage_right)
	p2.pos.x = clampf(p2.pos.x, p2.stage_left, p2.stage_right)


func _resolve_strikes(atk: FighterBody, dfd: FighterBody, dfd_inp: FightInput) -> void:
	if atk.state != FighterBody.State.ATTACK or atk.current_move == null:
		return
	if atk.move_has_hit:
		return
	if dfd.is_invulnerable() or dfd.state == FighterBody.State.KO:
		return
	for entry in atk.active_attack_boxes():
		var box: Aabb2 = entry["box"]
		var move: MoveDef = entry["move"]
		if not box.overlaps(dfd.world_hurtbox()):
			continue
		var blocked := _is_blocked(dfd, dfd_inp, move)
		dfd.take_hit(move, atk.facing, blocked)
		atk.mark_hit_connected()
		atk.hitstop = move.hitstop
		if not blocked:
			atk.add_meter(move.meter_gain_hit)
			dfd.add_meter(move.meter_gain_hit * 0.5)
		else:
			dfd.add_meter(4.0)
		last_event = "%s %s" % [move.move_id, "block" if blocked else "hit"]
		hit_connected.emit(atk.player_id, blocked, move.is_super())
		break


func _resolve_projectile_hits() -> void:
	for p in projectiles:
		if p.dead:
			continue
		var target: FighterBody = p2 if p.owner_id == 1 else p1
		var atk: FighterBody = p1 if p.owner_id == 1 else p2
		if target.is_invulnerable() or target.state == FighterBody.State.KO:
			continue
		if p.hit_targets.has(target.player_id):
			continue
		if not p.box().overlaps(target.world_hurtbox()):
			continue
		var blocked := target.state == FighterBody.State.BLOCK or target.state == FighterBody.State.BLOCKSTUN
		# Also check holding back via recent — use state BLOCK primarily
		target.take_hit(p.move, atk.facing, blocked)
		atk.add_meter(p.move.meter_gain_hit)
		p.hit_targets[target.player_id] = true
		p.dead = true
		last_event = "proj %s" % p.move.move_id
		hit_connected.emit(atk.player_id, blocked, p.move.is_super())


func _is_blocked(dfd: FighterBody, inp: FightInput, move: MoveDef) -> bool:
	if dfd.state in [FighterBody.State.HITSTUN, FighterBody.State.KNOCKDOWN, FighterBody.State.ATTACK, FighterBody.State.EVADE]:
		return false
	var holding_back := dfd.wants_block(inp) or dfd.state == FighterBody.State.BLOCK or dfd.state == FighterBody.State.BLOCKSTUN
	if not holding_back:
		return false
	if move.height == "low" and not dfd.crouching:
		return false
	if move.height == "overhead" and dfd.crouching:
		return false
	return true


func _training_locks() -> void:
	if not training:
		return
	if lock_hp:
		p1.hp = p1.max_hp
		p2.hp = p2.max_hp
	if lock_meter:
		meter1 = meter_max
		meter2 = meter_max


func _check_ko() -> void:
	if p1.state == FighterBody.State.KO and swap_cooldown <= 0:
		_on_ko(1)
	elif p2.state == FighterBody.State.KO and swap_cooldown <= 0:
		_on_ko(2)


func _on_ko(side: int) -> void:
	swap_cooldown = 30
	fighter_ko.emit(side)
	if side == 1:
		idx1 += 1
		if idx1 >= team1.size():
			frozen = true
			winner_side = 2
			match_over.emit(2)
			last_event = "P2 wins"
			return
		projectiles.clear()
		_spawn_side(1, false)
	else:
		idx2 += 1
		if idx2 >= team2.size():
			frozen = true
			winner_side = 1
			match_over.emit(1)
			last_event = "P1 wins"
			return
		projectiles.clear()
		_spawn_side(2, false)


func remaining(side: int) -> int:
	if side == 1:
		return team1.size() - idx1
	return team2.size() - idx2
