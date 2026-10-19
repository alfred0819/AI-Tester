extends Control

@onready var info: Label = $Info
@onready var grid: GridContainer = $Grid
@onready var p1_label: Label = $P1Pick
@onready var p2_label: Label = $P2Pick
@onready var confirm_btn: Button = $Confirm

var db := CharacterDB.new()
var p1_picks: Array[String] = []
var p2_picks: Array[String] = []
var selecting_p2: bool = false


func _ready() -> void:
	db.load_all()
	confirm_btn.disabled = true
	for cdef in db.all_chars():
		var btn := Button.new()
		btn.text = "%s\n%s" % [cdef.display_name, cdef.role]
		btn.custom_minimum_size = Vector2(180, 80)
		btn.modulate = cdef.color
		var id: String = cdef.char_id
		btn.pressed.connect(_on_pick.bind(id))
		grid.add_child(btn)
	_refresh()


func _on_pick(char_id: String) -> void:
	if GameSession.mode == GameSession.Mode.ARCADE:
		if p1_picks.size() < 3 and not p1_picks.has(char_id):
			p1_picks.append(char_id)
		_refresh()
		return

	if not selecting_p2:
		if p1_picks.size() < 3 and not p1_picks.has(char_id):
			p1_picks.append(char_id)
		if p1_picks.size() >= 3:
			selecting_p2 = true
	else:
		if p2_picks.size() < 3 and not p2_picks.has(char_id):
			p2_picks.append(char_id)
	_refresh()


func _refresh() -> void:
	p1_label.text = "P1 队序: %s" % (", ".join(p1_picks) if p1_picks else "（点选 3 人）")
	if GameSession.mode == GameSession.Mode.ARCADE:
		p2_label.text = "P2: CPU 关卡队"
		info.text = "Arcade：为 1P 选择 3 名不重复角色"
		confirm_btn.disabled = p1_picks.size() != 3
	else:
		var who := "P2" if selecting_p2 else "P1"
		info.text = "%s 选人中（各 3 人不重复）" % who
		p2_label.text = "P2 队序: %s" % (", ".join(p2_picks) if p2_picks else "（P1 选完后）")
		confirm_btn.disabled = not (p1_picks.size() == 3 and p2_picks.size() == 3)


func _on_confirm_pressed() -> void:
	if GameSession.mode == GameSession.Mode.ARCADE:
		GameSession.start_arcade(p1_picks)
	elif GameSession.mode == GameSession.Mode.TRAINING:
		GameSession.start_training(p1_picks, p2_picks)
	else:
		GameSession.start_versus(p1_picks, p2_picks)


func _on_back_pressed() -> void:
	get_tree().change_scene_to_file("res://scenes/title.tscn")


func _on_clear_pressed() -> void:
	p1_picks.clear()
	p2_picks.clear()
	selecting_p2 = false
	_refresh()
