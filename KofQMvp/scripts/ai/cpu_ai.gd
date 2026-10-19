class_name CpuAi
extends RefCounted
## Simple distance-based CPU that emits FightInput each tick.

var rng := RandomNumberGenerator.new()
var attack_cd: int = 0
var special_cd: int = 0
var think_timer: int = 0
var plan: String = "approach"


func _init() -> void:
	rng.randomize()


func think(me: FighterBody, foe: FighterBody) -> FightInput:
	var inp := FightInput.new()
	if attack_cd > 0:
		attack_cd -= 1
	if special_cd > 0:
		special_cd -= 1
	think_timer -= 1
	if think_timer <= 0:
		think_timer = rng.randi_range(20, 45)
		var roll := rng.randf()
		if roll < 0.45:
			plan = "approach"
		elif roll < 0.65:
			plan = "block"
		elif roll < 0.85:
			plan = "attack"
		else:
			plan = "special"

	var dx: float = foe.pos.x - me.pos.x
	var toward := 1 if dx > 0.0 else -1
	var dist := absf(dx)

	# Anti-air if foe airborne nearby
	if not foe.on_ground and dist < 120.0 and special_cd <= 0:
		inp.dir = 2
		_press(inp, FightInput.BTN_LP)
		special_cd = 90
		# Build 623 via dir history is hard in one frame; walk toward and punch
		inp.dir = 6 if toward > 0 else 4
		_press(inp, FightInput.BTN_LP)
		return inp

	match plan:
		"block":
			inp.dir = 4 if me.facing > 0 else 6
			if rng.randf() < 0.3:
				inp.dir = 1 if me.facing > 0 else 3
		"approach":
			inp.dir = 6 if toward > 0 else 4
			if dist < 70.0 and attack_cd <= 0:
				_press(inp, FightInput.BTN_LP)
				attack_cd = 18
		"attack":
			if dist > 90.0:
				inp.dir = 6 if toward > 0 else 4
			elif attack_cd <= 0:
				if rng.randf() < 0.5:
					_press(inp, FightInput.BTN_LP)
				else:
					_press(inp, FightInput.BTN_MP)
				attack_cd = 22
		"special":
			if special_cd <= 0:
				# Feed a short 236 motion over successive frames via dir + button
				inp.dir = 2
				if think_timer % 3 == 0:
					inp.dir = 3 if toward > 0 else 1
				if think_timer % 3 == 1:
					inp.dir = 6 if toward > 0 else 4
					_press(inp, FightInput.BTN_LP)
					special_cd = 100
			else:
				inp.dir = 6 if toward > 0 else 4
	return inp


func _press(inp: FightInput, mask: int) -> void:
	inp.buttons |= mask
	inp.pressed |= mask
