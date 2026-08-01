local State = {
    new = function()
        instance = { type = self }
        setmetatable(instance, { __index = self })
        return instance
    end
}

local TeleportState = {
    __index = State,
    
    enter = function(self, from_state) 
        print("ha")
    end,
    
    update = function(self) 
        print("he")
    end,
    
    exit = function(self, to_state)  
        print("hu")
    end
}