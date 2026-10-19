class_name FighterView
extends Node2D
## Procedural Q-style fighter silhouette (big head, short body).

var body: FighterBody
var show_boxes: bool = false
var flash: float = 0.0
var spark_timer: int = 0
var spark_block: bool = false


func setup(fighter: FighterBody) -> void:
	body = fighter
	queue_redraw()


func notify_hit(blocked: bool) -> void:
	flash = 0.35
	spark_timer = 10
	spark_block = blocked


func _process(delta: float) -> void:
	if flash > 0.0:
		flash = maxf(0.0, flash - delta * 2.0)
	if spark_timer > 0:
		spark_timer -= 1
	if body:
		position = Vector2(body.pos.x, body.pos.y)
		scale.x = float(body.facing)
		queue_redraw()


func _draw() -> void:
	if body == null or body.char_def == null:
		return
	var col: Color = body.char_def.color
	var accent: Color = body.char_def.accent
	if flash > 0.0:
		col = col.lerp(Color.WHITE, flash)
	if body.state == FighterBody.State.KO:
		col = col.darkened(0.45)

	# Shadow
	draw_ellipse(Vector2(0, 4), Vector2(36, 10), Color(0, 0, 0, 0.25))

	var crouch := body.crouching or body.state == FighterBody.State.CROUCH
	var head_y := -100.0 if not crouch else -70.0
	var body_h := 44.0 if not crouch else 28.0

	# Body
	draw_rect(Rect2(-18, head_y + 28, 36, body_h), col)
	# Head (Q)
	draw_circle(Vector2(0, head_y), 28, col)
	draw_circle(Vector2(0, head_y), 28, Color(0, 0, 0, 0), false, 2.0)
	# Eyes
	var eye := Color(0.1, 0.1, 0.12)
	draw_circle(Vector2(-10, head_y - 4), 4, eye)
	draw_circle(Vector2(10, head_y - 4), 4, eye)
	# Blush / accent cheeks
	draw_circle(Vector2(-16, head_y + 6), 5, Color(accent.r, accent.g, accent.b, 0.45))
	draw_circle(Vector2(16, head_y + 6), 5, Color(accent.r, accent.g, accent.b, 0.45))

	# Attack limb extension
	if body.state == FighterBody.State.ATTACK and body.current_move:
		var reach := 50.0
		if body.current_move.is_super():
			reach = 78.0
			draw_circle(Vector2(reach * 0.5, head_y), 22, Color(accent.r, accent.g, accent.b, 0.55))
		draw_rect(Rect2(12, head_y + 10, reach, 16), accent)
	elif body.state == FighterBody.State.EVADE:
		draw_circle(Vector2(0, head_y), 34, Color(1, 1, 1, 0.25))

	# Hit / block spark
	if spark_timer > 0:
		var sc := Color(1, 1, 0.4) if not spark_block else Color(0.5, 0.75, 1.0)
		draw_circle(Vector2(40, head_y + 10), 8 + spark_timer, sc)

	if show_boxes and body:
		var hurt := body.world_hurtbox()
		var local_hurt := Rect2(hurt.x - body.pos.x, hurt.y - body.pos.y, hurt.w, hurt.h)
		# Unflip for draw because node scale.x mirrors
		if body.facing < 0:
			local_hurt.position.x = -(local_hurt.position.x + local_hurt.size.x)
		draw_rect(local_hurt, Color(0.2, 1, 0.3, 0.25))
		for hb in body.debug_hitboxes:
			var r := Rect2(hb.x - body.pos.x, hb.y - body.pos.y, hb.w, hb.h)
			if body.facing < 0:
				r.position.x = -(r.position.x + r.size.x)
			draw_rect(r, Color(1, 0.2, 0.2, 0.35))


func draw_ellipse(center: Vector2, radii: Vector2, color: Color) -> void:
	var pts := PackedVector2Array()
	for i in 16:
		var a := TAU * float(i) / 16.0
		pts.append(center + Vector2(cos(a) * radii.x, sin(a) * radii.y))
	draw_colored_polygon(pts, color)
