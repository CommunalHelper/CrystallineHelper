local bombTimer = {}

bombTimer.name = "vitellary/bombtimer"

bombTimer.fieldInformation = {
    startDirection = {
        editable = false,
        options = {
            "Any",
            "Right",
            "Left",
            "Up",
            "Down",
        },
    },
    timer = {
        minimumValue = 0,
    },
    soundAt = {
        minimumValue = 0,
        maximumValue = function(room, entity)
            return entity.timer
        end,
    },
}

bombTimer.placements = {
    {
        name = "bomb_timer",
        data = {
            sound = "",
            soundAt = 0,
            timer = 0,
            startDirection = "Any",
            changeRespawn = true,
            resetOnDeath = true,
        }
    }
}

return bombTimer