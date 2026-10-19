class_name CharacterDef
extends RefCounted

var char_id: String = ""
var display_name: String = ""
var role: String = ""
var color: Color = Color.WHITE
var accent: Color = Color.WHITE
var max_hp: int = 1000
var walk_speed: float = 220.0
var back_walk_speed: float = 180.0
var jump_speed: float = 520.0
var gravity: float = 1600.0
var hurt := Aabb2.new(-28, -120, 56, 120)
var moves: Array[MoveDef] = []
var move_by_id: Dictionary = {}
var move_by_command: Dictionary = {}


func get_move(move_id: String) -> MoveDef:
	return move_by_id.get(move_id, null)


func get_by_command(cmd: String) -> MoveDef:
	return move_by_command.get(cmd, null)


static func from_dict(d: Dictionary) -> CharacterDef:
	var c := CharacterDef.new()
	c.char_id = str(d.get("char_id", ""))
	c.display_name = str(d.get("display_name", c.char_id))
	c.role = str(d.get("role", ""))
	var col: Array = d.get("color", [1, 1, 1])
	c.color = Color(float(col[0]), float(col[1]), float(col[2]))
	var acc: Array = d.get("accent", col)
	c.accent = Color(float(acc[0]), float(acc[1]), float(acc[2]))
	c.max_hp = int(d.get("max_hp", 1000))
	c.walk_speed = float(d.get("walk_speed", 220))
	c.back_walk_speed = float(d.get("back_walk_speed", 180))
	c.jump_speed = float(d.get("jump_speed", 520))
	c.gravity = float(d.get("gravity", 1600))
	var h: Array = d.get("hurt", [-28, -120, 56, 120])
	c.hurt = Aabb2.new(float(h[0]), float(h[1]), float(h[2]), float(h[3]))
	for md in d.get("moves", []):
		var move := MoveDef.from_dict(md)
		c.moves.append(move)
		c.move_by_id[move.move_id] = move
		c.move_by_command[move.command] = move
	return c
