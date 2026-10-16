class_name FightInput
extends RefCounted
## One-frame sampled input for a fighter (numpad dir + buttons).

const BTN_LP := 1
const BTN_MP := 2
const BTN_HP := 4
const BTN_LK := 8
const BTN_MK := 16
const BTN_HK := 32

var dir: int = 5
var buttons: int = 0
var pressed: int = 0
var released: int = 0


func is_held(mask: int) -> bool:
	return (buttons & mask) != 0


func just_pressed(mask: int) -> bool:
	return (pressed & mask) != 0
