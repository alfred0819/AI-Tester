extends Control


func _on_versus_pressed() -> void:
	GameSession.mode = GameSession.Mode.VERSUS
	GameSession.p2_is_cpu = false
	get_tree().change_scene_to_file("res://scenes/char_select.tscn")


func _on_training_pressed() -> void:
	GameSession.mode = GameSession.Mode.TRAINING
	GameSession.p2_is_cpu = false
	get_tree().change_scene_to_file("res://scenes/char_select.tscn")


func _on_arcade_pressed() -> void:
	GameSession.mode = GameSession.Mode.ARCADE
	GameSession.p2_is_cpu = true
	get_tree().change_scene_to_file("res://scenes/char_select.tscn")
