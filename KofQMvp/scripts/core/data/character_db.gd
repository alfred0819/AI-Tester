class_name CharacterDB
extends RefCounted

const CHAR_FILES := [
	"res://data/characters/C01_akazan.json",
	"res://data/characters/C02_qingying.json",
	"res://data/characters/C03_yanyue.json",
	"res://data/characters/C04_lingfeng.json",
]

var _chars: Dictionary = {}
var balance: Dictionary = {}


func load_all() -> void:
	_chars.clear()
	for path in CHAR_FILES:
		var text := FileAccess.get_file_as_string(path)
		var data: Variant = JSON.parse_string(text)
		if typeof(data) != TYPE_DICTIONARY:
			push_error("Bad character JSON: %s" % path)
			continue
		var cdef := CharacterDef.from_dict(data)
		_chars[cdef.char_id] = cdef
	var bal_text := FileAccess.get_file_as_string("res://data/balance/defaults.json")
	var bal: Variant = JSON.parse_string(bal_text)
	balance = bal if typeof(bal) == TYPE_DICTIONARY else {}


func all_ids() -> Array:
	var ids: Array = _chars.keys()
	ids.sort()
	return ids


func get_char(char_id: String) -> CharacterDef:
	return _chars.get(char_id, null)


func all_chars() -> Array:
	var out: Array = []
	for id in all_ids():
		out.append(_chars[id])
	return out
