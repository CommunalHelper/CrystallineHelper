module FlushelineDropHoldableTrigger

using ..Ahorn, Maple

@mapdef Trigger "vitellary/dropholdables" DropHoldableTrigger(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
	"Drop Holdable Trigger (Crystalline)" => Ahorn.EntityPlacement(
        DropHoldableTrigger,
        "rectangle"
    )
)
end