extends Node2D
## Phase 0 arena: runs FightSimulator and draws greybox fighters.

@onready var camera: Camera2D = $Camera2D
@onready var p1_view: ColorRect = $P1View
@onready var p2_view: ColorRect = $P2View
@onready var hud: Label = $CanvasLayer/Hud

var sim := FightSimulator.new()
var _prev_p1_buttons: int = 0
var _prev_p2_buttons: int = 0


func _ready() -> void:
	sim.setup_default()
	_sync_views()


func _physics_process(_delta: float) -> void:
	var i1 := ActionSampler.sample("p1")
	var i2 := ActionSampler.sample("p2")
	i1.pressed = i1.buttons & ~_prev_p1_buttons
	i1.released = _prev_p1_buttons & ~i1.buttons
	i2.pressed = i2.buttons & ~_prev_p2_buttons
	i2.released = _prev_p2_buttons & ~i2.buttons
	_prev_p1_buttons = i1.buttons
	_prev_p2_buttons = i2.buttons

	sim.step(i1, i2)
	_sync_views()
	_update_camera()
	hud.text = "KofQMvp Phase 0 | tick %d | WASD+J/K vs Arrows+Num0/." % sim.tick


func _sync_views() -> void:
	_place_view(p1_view, sim.p1, Color(0.94, 0.45, 0.28))
	_place_view(p2_view, sim.p2, Color(0.25, 0.55, 0.85))


func _place_view(rect: ColorRect, body: FighterBody, color: Color) -> void:
	rect.color = color
	rect.size = Vector2(body.hurt.w, body.hurt.h)
	rect.position = Vector2(body.pos.x - body.hurt.w * 0.5, body.pos.y - body.hurt.h)


func _update_camera() -> void:
	var mid_x: float = (sim.p1.pos.x + sim.p2.pos.x) * 0.5
	camera.position = Vector2(clampf(mid_x, 480.0, 800.0), 360.0)


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		get_tree().change_scene_to_file("res://scenes/title.tscn")
