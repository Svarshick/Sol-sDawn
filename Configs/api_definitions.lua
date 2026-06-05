---@meta

---@class Vector2
---@field X number
---@field Y number
local Vector2 = {}

---@class OrbStats
local OrbStats = {}

---@class Color
local Color = {}

---Exposes the custom Recoil Orb configuration metrics.
---@type OrbStats
AlnoraRecoilOrbsStats = nil

---Exposes the default Orb configuration metrics.
---@type OrbStats
DefaultOrbStats = nil

---Creates a basic raw Vector2 position representation.
---@param x number
---@param y number
---@return Vector2
function CreateVector(x, y) end

---Translates design layout units into pixel space values.
---@param x number
---@param y number
---@return Vector2
function Units(x, y) end

---Converts a layout units float value to game pixels.
---@param units number
---@return number
function UnitsFloat(units) end

---Rotates an vector layout by a radial angle direction.
---@param vector Vector2
---@param radians number
---@return Vector2
function Rotate(vector, radians) end

---Returns a normalized 1-unit scale vector pointing in the original direction.
---@param vector Vector2
---@return Vector2
function Normalize(vector) end

---Retrieves the real-time position coordinate of the Player.
---@return Vector2
function GetPlayerPosition() end

---Retrieves the real-time position coordinate of the Boss.
---@return Vector2
function GetBossPosition() end

---Retrieves the central location point of the screen layout view.
---@return Vector2
function GetCameraCenter() end

---Retrieves the top-left coordinate corner of the screen layout view.
---@return Vector2
function GetCameraTopLeft() end

---Retrieves the top-right coordinate corner of the screen layout view.
---@return Vector2
function GetCameraTopRight() end

---Retrieves the bottom-left coordinate corner of the screen layout view.
---@return Vector2
function GetCameraBottomLeft() end

---Retrieves the bottom-right coordinate corner of the screen layout view.
---@return Vector2
function GetCameraBottomRight() end

---Determines if the Boss's last blade usage was successfully parried.
---@return boolean
function IsBossLastBladeParried() end

---Determines if the Boss's last fire usage was successfully parried.
---@return boolean
function IsBossLastFireParried() end

---Determines if the Boss's last blade usage successfully reached the target.
---@return boolean
function IsBossLastBladeSuccess() end

---Determines if the Boss's last fire usage successfully reached the target.
---@return boolean
function IsBossLastFireSuccess() end

---Pauses script execution on the boss for the specified duration.
---@param seconds number
function Wait(seconds) end

---Schedules a teleport action.
---@param targetPosition Vector2
function Teleport(targetPosition) end

---Schedules a Blade attack aiming at a target position.
---@param targetPosition Vector2
function Blade(targetPosition) end

---Schedules a Fire execution aiming at a target position.
---@param targetPosition Vector2
function Fire(targetPosition) end

---Instantly triggers an orb generation action.
---@param startPosition Vector2
---@param targetPosition Vector2
---@param stats OrbStats
function SpawnOrb(startPosition, targetPosition, stats) end
