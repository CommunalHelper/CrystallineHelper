local flagSequenceController = {}

flagSequenceController.name = "vitellary/flagsequencecontroller"
flagSequenceController.depth = -100

flagSequenceController.placements = {
    {
        name = "flag_sequence_controller",
        data = {
            prefix = "",
            state = false,
            startNumber = 1,
            endNumber = 99
        }
    }
}

flagSequenceController.texture = "ahorn_flagsequencecontroller"

return flagSequenceController
