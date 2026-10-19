extends Control

@onready var label: Label = $Label


func _ready() -> void:
	if GameSession.mode == GameSession.Mode.ARCADE:
		if GameSession.arcade_won:
			label.text = "Arcade Clear!\n击败全部 %d 关" % GameSession.ARCADE_OPPONENTS.size()
		else:
			label.text = "Arcade Failed\n停在第 %d 关" % (GameSession.arcade_stage + 1)
	else:
		var w := GameSession.last_winner_side
		label.text = ("P%d 获胜！\n回标题再战" % w) if w > 0 else "对战结束\n回标题"


func _on_title_pressed() -> void:
	get_tree().change_scene_to_file("res://scenes/title.tscn")
