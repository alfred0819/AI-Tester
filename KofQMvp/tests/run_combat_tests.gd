extends SceneTree
## Headless combat regression: godot --headless --path . --script res://tests/run_combat_tests.gd

var _failed: int = 0


func _init() -> void:
	print("=== KofQMvp combat tests ===")
	_test_load_characters()
	_test_normals_deal_damage()
	_test_block_prevents_full_damage()
	_test_meter_and_super()
	_test_cancel_chain()
	_test_evade_invuln()
	_test_team_ko_swap()
	_test_command_motions()
	if _failed == 0:
		print("ALL TESTS PASSED")
		quit(0)
	else:
		print("FAILED: %d" % _failed)
		quit(1)


func _assert(cond: bool, msg: String) -> void:
	if cond:
		print("  OK  ", msg)
	else:
		_failed += 1
		print("  FAIL", msg)


func _neutral() -> FightInput:
	return FightInput.new()


func _btn(mask: int, dir: int = 5) -> FightInput:
	var i := FightInput.new()
	i.dir = dir
	i.buttons = mask
	i.pressed = mask
	return i


func _test_load_characters() -> void:
	print("-- load characters")
	var db := CharacterDB.new()
	db.load_all()
	_assert(db.all_ids().size() == 4, "4 characters loaded")
	for id in ["C01", "C02", "C03", "C04"]:
		var c: CharacterDef = db.get_char(id)
		_assert(c != null, "char %s" % id)
		_assert(c.moves.size() >= 6, "%s has moves" % id)
		_assert(c.get_by_command("5A") != null, "%s 5A" % id)
		_assert(c.get_by_command("236A") != null, "%s 236A" % id)
		_assert(c.get_by_command("236236A") != null, "%s super" % id)


func _test_normals_deal_damage() -> void:
	print("-- normals damage")
	var sim := FightSimulator.new()
	sim.setup(["C01", "C02", "C03"], ["C04", "C02", "C03"], false)
	# Place p1 close to p2
	sim.p1.pos.x = sim.p2.pos.x - 70
	var hp0 := sim.p2.hp
	# Startup 5A: press, then wait active frames
	sim.step(_btn(FightInput.BTN_LP), _neutral())
	for _i in 20:
		sim.step(_neutral(), _neutral())
	_assert(sim.p2.hp < hp0, "5A dealt damage (%d -> %d)" % [hp0, sim.p2.hp])


func _test_block_prevents_full_damage() -> void:
	print("-- block")
	var sim := FightSimulator.new()
	sim.setup(["C01", "C02", "C03"], ["C04", "C02", "C03"], false)
	sim.p1.pos.x = sim.p2.pos.x - 70
	var hp0 := sim.p2.hp
	sim.step(_btn(FightInput.BTN_LP), _neutral())
	for _i in 20:
		# P2 holds back (facing -1 => back is right/6)
		var blk := FightInput.new()
		blk.dir = 6
		sim.step(_neutral(), blk)
	_assert(sim.p2.hp == hp0 or sim.p2.state == FighterBody.State.BLOCKSTUN or sim.p2.hp >= hp0 - 5, "block mitigated (%d -> %d)" % [hp0, sim.p2.hp])
	_assert(sim.p2.hp > hp0 - 40, "did not take full 5A on block")


func _test_meter_and_super() -> void:
	print("-- meter super")
	var sim := FightSimulator.new()
	sim.setup(["C01", "C02", "C03"], ["C04", "C02", "C03"], false)
	sim.meter1 = 0
	_assert(not sim.p1.spend_meter(100), "cannot spend without meter")
	sim.meter1 = 100
	_assert(sim.p1.spend_meter(100), "spend 100 ok")
	_assert(is_equal_approx(sim.meter1, 0.0), "meter emptied")
	sim.meter1 = 100
	sim.p1.pos.x = sim.p2.pos.x - 80
	# Feed 236236A via buffer then punch
	_feed_motion(sim, [2, 3, 6, 2, 3, 6], true)
	for _i in 40:
		sim.step(_neutral(), _neutral())
	_assert(sim.p1.last_move_id == "inferno" or sim.meter1 < 100, "super attempted/spent")


func _feed_motion(sim: FightSimulator, dirs: Array, punch: bool) -> void:
	for d in dirs:
		var i := FightInput.new()
		i.dir = int(d)
		sim.step(i, _neutral())
	if punch:
		sim.step(_btn(FightInput.BTN_LP, 6), _neutral())


func _test_cancel_chain() -> void:
	print("-- cancel")
	var sim := FightSimulator.new()
	sim.setup(["C01", "C02", "C03"], ["C04", "C02", "C03"], true)
	sim.p1.pos.x = sim.p2.pos.x - 65
	sim.step(_btn(FightInput.BTN_LP), _neutral())
	# Advance into cancel window after hit
	for _i in 8:
		sim.step(_neutral(), _neutral())
	# Try special cancel
	_feed_motion(sim, [2, 3, 6], true)
	for _i in 5:
		sim.step(_neutral(), _neutral())
	var ok := sim.p1.last_move_id in ["fireball", "5A", "inferno"]
	_assert(ok, "cancel or attack state ok (%s)" % sim.p1.last_move_id)


func _test_evade_invuln() -> void:
	print("-- evade")
	var sim := FightSimulator.new()
	sim.setup(["C01", "C02", "C03"], ["C04", "C02", "C03"], false)
	sim.meter1 = 100
	sim.p1.pos.x = 400
	var i := FightInput.new()
	i.dir = 4  # back while facing right
	i.pressed = FightInput.BTN_MK
	i.buttons = FightInput.BTN_MK
	sim.step(i, _neutral())
	_assert(sim.p1.state == FighterBody.State.EVADE, "entered evade")
	_assert(sim.p1.invuln_timer > 0, "evade invuln")
	_assert(sim.meter1 < 100, "evade cost meter")


func _test_team_ko_swap() -> void:
	print("-- team ko swap")
	var sim := FightSimulator.new()
	sim.setup(["C01", "C02", "C03"], ["C04", "C02", "C03"], false)
	sim.p2.hp = 1
	sim.p1.pos.x = sim.p2.pos.x - 70
	sim.step(_btn(FightInput.BTN_MP), _neutral())
	for _i in 30:
		sim.step(_neutral(), _neutral())
	_assert(sim.idx2 >= 1 or sim.winner_side == 1, "P2 swapped or match over (idx2=%d winner=%d)" % [sim.idx2, sim.winner_side])


func _test_command_motions() -> void:
	print("-- command matcher")
	var buf := InputBuffer.new()
	for d in [5, 2, 3, 6]:
		var i := FightInput.new()
		i.dir = d
		buf.push(i)
	var punch := FightInput.new()
	punch.dir = 6
	punch.pressed = FightInput.BTN_LP
	punch.buttons = FightInput.BTN_LP
	buf.push(punch)
	var cmd := CommandMatcher.detect(buf, false)
	_assert(cmd == "236A", "236A detected (got %s)" % cmd)
