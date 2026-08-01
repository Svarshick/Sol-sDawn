function test()  
    local position = vector(0, 0)
    local red = color(257, 0, 0, 0)
    local circle = animation.circle(position, 1, 20, 1, red)
    local speed = 1
    while true do
        local time = get_time()
        local delta = vector(input.move.x * speed * time, input.move.y * speed * time)
        circle.position = circle.position + delta;
        coroutine.yield();
    end
end 