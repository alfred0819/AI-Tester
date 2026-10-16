extends Control


func _on_versus_pressed() -> void:
	get_tree().change_scene_to_file("res://scenes/fight.tscn")


func _on_training_pressed() -> void:
	# Phase 4 will branch; for now same greybox fight.
	get_tree().change_scene_to_file("res://scenes/fight.tscn")


func _on_arcade_pressed() -> void:
	get_tree().change_scene_to_file("res://scenes/fight.tscn")
