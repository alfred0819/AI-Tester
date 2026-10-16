extends Node
## Global match setup shared across title / select / fight.

enum Mode { VERSUS, TRAINING, ARCADE }

var mode: Mode = Mode.VERSUS
var p1_team: Array[String] = ["C01", "C02", "C03"]
var p2_team: Array[String] = ["C04", "C03", "C02"]
var arcade_stage: int = 0
var arcade_won: bool = false
var p2_is_cpu: bool = false
var last_winner_side: int = 0

const ARCADE_OPPONENTS := [
	["C02", "C04", "C01"],
	["C03", "C01", "C04"],
	["C04", "C02", "C03"],
	["C01", "C03", "C02"],
]


func start_versus(t1: Array, t2: Array) -> void:
	mode = Mode.VERSUS
	p1_team = _copy_ids(t1)
	p2_team = _copy_ids(t2)
	p2_is_cpu = false
	get_tree().change_scene_to_file("res://scenes/fight.tscn")


func start_training(t1: Array, t2: Array) -> void:
	mode = Mode.TRAINING
	p1_team = _copy_ids(t1)
	p2_team = _copy_ids(t2)
	p2_is_cpu = false
	get_tree().change_scene_to_file("res://scenes/fight.tscn")


func start_arcade(t1: Array) -> void:
	mode = Mode.ARCADE
	p1_team = _copy_ids(t1)
	arcade_stage = 0
	arcade_won = false
	p2_is_cpu = true
	_load_arcade_opponent()
	get_tree().change_scene_to_file("res://scenes/fight.tscn")


func _load_arcade_opponent() -> void:
	var opp: Array = ARCADE_OPPONENTS[clampi(arcade_stage, 0, ARCADE_OPPONENTS.size() - 1)]
	p2_team = _copy_ids(opp)


func advance_arcade_or_finish(p1_won: bool) -> void:
	if not p1_won:
		arcade_won = false
		get_tree().change_scene_to_file("res://scenes/result.tscn")
		return
	arcade_stage += 1
	if arcade_stage >= ARCADE_OPPONENTS.size():
		arcade_won = true
		get_tree().change_scene_to_file("res://scenes/result.tscn")
		return
	_load_arcade_opponent()
	get_tree().change_scene_to_file("res://scenes/fight.tscn")


func _copy_ids(src: Array) -> Array[String]:
	var out: Array[String] = []
	for id in src:
		out.append(str(id))
	return out
