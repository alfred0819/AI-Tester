class_name ActionSampler
extends RefCounted
## Maps Godot InputMap actions to FightInput for P1/P2.


static func sample(prefix: String) -> FightInput:
	var inp := FightInput.new()
	var left := Input.is_action_pressed("%s_left" % prefix)
	var right := Input.is_action_pressed("%s_right" % prefix)
	var up := Input.is_action_pressed("%s_up" % prefix)
	var down := Input.is_action_pressed("%s_down" % prefix)

	var x := 0
	if left:
		x -= 1
	if right:
		x += 1
	var y := 0
	if up:
		y += 1
	if down:
		y -= 1

	inp.dir = _dir_from_axes(x, y)

	var held := 0
	if Input.is_action_pressed("%s_lp" % prefix):
		held |= FightInput.BTN_LP
	if Input.is_action_pressed("%s_mp" % prefix):
		held |= FightInput.BTN_MP
	if Input.is_action_pressed("%s_lk" % prefix):
		held |= FightInput.BTN_LK
	if Input.is_action_pressed("%s_mk" % prefix):
		held |= FightInput.BTN_MK
	inp.buttons = held
	return inp


static func _dir_from_axes(x: int, y: int) -> int:
	## x: -1..1, y: -1..1 where +y is up (numpad)
	if x < 0 and y > 0:
		return 7
	if x == 0 and y > 0:
		return 8
	if x > 0 and y > 0:
		return 9
	if x < 0 and y == 0:
		return 4
	if x == 0 and y == 0:
		return 5
	if x > 0 and y == 0:
		return 6
	if x < 0 and y < 0:
		return 1
	if x == 0 and y < 0:
		return 2
	if x > 0 and y < 0:
		return 3
	return 5
