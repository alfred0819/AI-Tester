extends SceneTree
func _init() -> void:
	var scenes := [
		"res://scenes/boot.tscn",
		"res://scenes/title.tscn",
		"res://scenes/char_select.tscn",
		"res://scenes/fight.tscn",
		"res://scenes/result.tscn",
	]
	var failed := 0
	for path in scenes:
		var ps := load(path)
		if ps == null:
			print("FAIL load ", path)
			failed += 1
		else:
			var n = ps.instantiate()
			if n == null:
				print("FAIL inst ", path)
				failed += 1
			else:
				print("OK ", path)
				n.free()
	quit(1 if failed else 0)
