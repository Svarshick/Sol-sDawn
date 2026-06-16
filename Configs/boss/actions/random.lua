while true do
    print("bla2")
    t0 = timer(1)
    t1 = t0.after(1)
    t0.onFire(function() print('t0 end')  end)
    wait(t1)
    run("actions/color_test")
end 