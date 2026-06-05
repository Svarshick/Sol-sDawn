-- Utility actions
local function RadiumRecoil()
    local bossPos = GetBossPosition()
    local playerPos = GetPlayerPosition()
    local direction = Normalize(bossPos - playerPos)
    SpawnOrb(bossPos, direction * UnitsFloat(2) * 100, AlnoraRecoilOrbsStats)
end

local function AVSnipeShot()
    RadiumRecoil()
    Fire(GetPlayerPosition())
    Wait(0.5)
end

local function FireTeacher()
    local cameraPositions = {
        GetCameraBottomLeft(),
        GetCameraBottomRight(),
        GetCameraTopRight(),
        GetCameraTopLeft()
    }

    for i = 1, #cameraPositions do
        local camPoint = cameraPositions[i]
        local playerPos = GetPlayerPosition()
        
        -- Teleport 3/4 distance from player to corner
        local targetPos = playerPos + (camPoint - playerPos) * 0.75
        Teleport(targetPos)
        Wait(1.2)
        AVSnipeShot()
    end
    
    Wait(0.5)
end

-- Main behavior loop
while true do
    FireTeacher()
    Wait(0.5)
end
