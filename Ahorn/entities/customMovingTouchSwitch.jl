module FlushelineCustomMovingTouchSwitch

using ..Ahorn, Maple

@mapdef Entity "vitellary/customtouchswitch" CustomMovingTouchSwitch(x::Integer, y::Integer, flag::String="", inactiveColor::String="5FCDE4", movingColor::String="FF7F7F", activeColor::String="FFFFFF", finishColor::String="F141DF", moveTime::Number=1.25, easing::String="CubeOut", icon::String="vanilla", inverted::Bool=false, persistent::Bool=false, smoke::Bool=true, allowDisable::Bool=true, badelineDeactivate::Bool=false)

const placements = Ahorn.PlacementDict(
    "Custom Touch Switch (Crystalline)" => Ahorn.EntityPlacement(
        CustomMovingTouchSwitch
    )
)

const easeTypes = String["Linear", "SineIn", "SineOut", "SineInOut", "QuadIn", "QuadOut", "QuadInOut", "CubeIn", "CubeOut", "CubeInOut", "QuintIn", "QuintOut", "QuintInOut", "BackIn", "BackOut", "BackInOut", "ExpoIn", "ExpoOut", "ExpoInOut", "BigBackIn", "BigBackOut", "BigBackInOut", "ElasticIn", "ElasticOut", "ElasticInOut", "BounceIn", "BounceOut", "BounceInOut"]

const iconTypes = String["vanilla", "circle", "tall", "triangle"]

Ahorn.nodeLimits(entity::CustomMovingTouchSwitch) = 0, -1
Ahorn.editingOptions(entity::CustomMovingTouchSwitch) = Dict{String, Any}(
    "easing" => easeTypes,
    "icon" => iconTypes
)
Ahorn.editingOrder(entity::CustomMovingTouchSwitch) = String["x", "y", "inactiveColor", "activeColor", "movingColor", "finishColor", "flag", "moveTime", "icon", "easing", "allowDisable", "inverted", "persistent", "smoke", "badelineDeactivate", "nodes"]

function Ahorn.selection(entity::CustomMovingTouchSwitch)
    nodes = get(entity.data, "nodes", ())
    x, y = Ahorn.position(entity)

    sprite = "objects/touchswitch/container.png"

    res = Ahorn.Rectangle[Ahorn.getSpriteRectangle(sprite, x, y)]
    
    for node in nodes
        nx, ny = Int.(node)

        push!(res, Ahorn.getSpriteRectangle(sprite, nx, ny))
    end

    return res
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::CustomMovingTouchSwitch)
    px, py = Ahorn.position(entity)

    sprite = "objects/touchswitch/container.png"

    for node in get(entity.data, "nodes", ())
        nx, ny = Int.(node)

        theta = atan(py - ny, px - nx)
        Ahorn.drawArrow(ctx, px, py, nx + cos(theta) * 8, ny + sin(theta) * 8, Ahorn.colors.selection_selected_fc, headLength=6)
        Ahorn.drawSprite(ctx, sprite, nx, ny)

        px, py = nx, ny
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CustomMovingTouchSwitch)
    icon = get(entity.data, "icon", "vanilla")

    sprite = "objects/touchswitch/icon00.png"
    if icon != "vanilla"
        sprite = "objects/customMovingTouchSwitch/"*icon*"/icon00.png"
    end

    Ahorn.drawSprite(ctx, "objects/touchswitch/container.png", 0, 0)
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end