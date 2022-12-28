local interactiveChaser = {}

interactiveChaser.name = "vitellary/interactivechaser"
interactiveChaser.depth = 0
interactiveChaser.justification = {0.5, 1.0}
interactiveChaser.texture = "characters/badeline/sleep00"
interactiveChaser.placements = {
    name = "interactive_chaser",
    data = {
        followDelay = 1.5,
        startDelay = 0.0,
        mirroring = "None",
        flag = "",
        blacklist = "",
        harmful = true,
        canChangeMusic = false,
    }
}
interactiveChaser.fieldInformation = {
    mirroring = {
        editable = false,
        options = {"None", "FlipH", "FlipV", "FlipBoth"},
    },
}

function interactiveChaser.scale(room, entity)
    local flip = entity.mirroring
    return (flip == "FlipH" or flip == "FlipBoth") and -1 or 1, (flip == "FlipV" or flip == "FlipBoth") and -1 or 1
end

return interactiveChaser