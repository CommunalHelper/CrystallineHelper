module FlushelineBombTimerTrigger

using ..Ahorn, Maple

@mapdef Trigger "vitellary/bombtimer" BombTimerTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16, sound::String="", soundAt::Number=0.0, timer::Number=0.0, startDirection::String="Any", changeRespawn::Bool=true, resetOnDeath::Bool=true)

const placements = Ahorn.PlacementDict(
    "Bomb Timer Trigger (Crystalline)" => Ahorn.EntityPlacement(
        BombTimerTrigger,
        "rectangle"
    )
)

const startDirections = String["Any", "Right", "Left", "Up", "Down"]

Ahorn.editingOptions(trigger::BombTimerTrigger) = Dict{String, Any}(
    "startDirection" => startDirections
)

end