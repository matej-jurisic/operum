import { CollisionDetection } from "@dnd-kit/core";

export const closestToPointer: CollisionDetection = ({
    pointerCoordinates,
    droppableContainers,
}) => {
    if (!pointerCoordinates) {
        return [];
    }

    const collisions = [];

    for (const droppableContainer of droppableContainers) {
        const rect = droppableContainer.rect.current;

        if (!rect) {
            continue;
        }

        // Calculate the center of the droppable container
        const centerX = rect.left + rect.width / 2;
        const centerY = rect.top + rect.height / 2;

        // Calculate distance from pointer to center
        const distanceX = pointerCoordinates.x - centerX;
        const distanceY = pointerCoordinates.y - centerY;
        const distance = Math.sqrt(
            distanceX * distanceX + distanceY * distanceY
        );

        collisions.push({
            id: droppableContainer.id,
            data: { droppableContainer, value: distance },
        });
    }
    return collisions.sort((a, b) => a.data.value - b.data.value);
};
