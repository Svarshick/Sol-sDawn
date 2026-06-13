while true do 
    print("bla2")
    t0 = timer(1)
    wait(t0)
    t1 = t0.after(1)
    print('t0 end')
    wait(t1)
    run("actions/boss_actions")
end