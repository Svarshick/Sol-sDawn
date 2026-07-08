scale = 1

attack_position = vector(0, 0)
attack_radius = 1
warning_animation_color = color(255, 255, 255)
parry_animation_color = color(0, 255, 0)

while true do
    
    warning_animation = animation.circle(
        attack_position,
        attack_radius, 
        20, 
        attack_radius, 
        warning_animation_color,
        0)

    pw_shape = shape.circle(attack_radius)
    pw = parryWindow(pw_shape, function() return true end, function() return true end)
    pw.transform.position = attack_position
    pw_t = timer(1 * scale)
    pw_t.onFire(function() pw:open() warning_animation.color = parry_animation_color end)

    atk_t = pw_t:after(1 * scale)

    branch = race(pw.parried, atk_t)
    branch:onEnd(function() warning_animation:cancel() pw:destroy() end)

    branch:onWinner(pw.parried):onFire(function() print("PARRY") end)
    branch:onWinner(atk_t):onFire(function() print("ATTACK") end)

    wait(5 * scale)
end 