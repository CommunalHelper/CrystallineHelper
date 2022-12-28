module FlushelineInteractiveChaser

using ..Ahorn, Maple

@mapdef Entity "vitellary/interactivechaser" InteractiveChaser(x::Integer, y::Integer, followDelay::Number=1.5, startDelay::Number=0.0, mirroring::String="None", flag::String="", blacklist::String="", harmful::Bool=true, canChangeMusic::Bool=false)

const placements = Ahorn.PlacementDict(
    "Interactive Badeline Chaser (Crystalline)" => Ahorn.EntityPlacement(
        InteractiveChaser
    )
)

const mirrorOptions = String["None", "FlipH", "FlipV", "FlipBoth"]

Ahorn.editingOptions(entity::InteractiveChaser) = Dict{String, Any}(
    "mirroring" => mirrorOptions
)

# This sprite fits best, not perfect, thats why we have a offset here
chaserSprite = "characters/badeline/sleep00.png"

function Ahorn.selection(entity::InteractiveChaser)
    x, y = Ahorn.position(entity)
    mirroring = get(entity.data, "mirroring", "None")

    ox = (mirroring == "FlipH" || mirroring == "FlipBoth" ? 0 : 4)
    sx = (mirroring == "FlipH" || mirroring == "FlipBoth" ? -1.0 : 1.0)
    sy = (mirroring == "FlipV" || mirroring == "FlipBoth" ? -1.0 : 1.0)
    
    return Ahorn.getSpriteRectangle(chaserSprite, x + ox, y, jx=0.5, jy=1.0, sx=sx, sy=sy)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::InteractiveChaser)
    mirroring = get(entity.data, "mirroring", "None")

    ox = (mirroring == "FlipH" || mirroring == "FlipBoth" ? 0 : 4)
    sx = (mirroring == "FlipH" || mirroring == "FlipBoth" ? -1.0 : 1.0)
    sy = (mirroring == "FlipV" || mirroring == "FlipBoth" ? -1.0 : 1.0)

    Ahorn.drawSprite(ctx, chaserSprite, ox, 0, jx=0.5, jy=1.0, sx=sx, sy=sy)
end

end