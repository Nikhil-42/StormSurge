import cv2
from itertools import product
import numpy as np
import json

borders = cv2.cvtColor(cv2.imread("equirectangularprojection.png"), cv2.COLOR_BGR2HSV)
mask = cv2.inRange(borders, (27, 23, 252), (29, 25, 255))
red = np.full(borders.shape, (0, 0, 255), dtype=borders.dtype)
borders = cv2.bitwise_and(red, red, mask=mask)

dragging = False
def on_click(event, x, y, flags, param):
    global dragging
    match event:
        case cv2.EVENT_LBUTTONDOWN:
            dragging = True
        case cv2.EVENT_LBUTTONUP:
            dragging = False

    if dragging:
        for dx, dy in product((-1, 0, 1), (-1, 0, 1)):
            point = (min(max(0, x + dx), borders.shape[1] - 1), min(max(0, y + dy), borders.shape[0] - 1))
            if (any(borders[*point[::-1]] != [0, 0, 0])):
                cv2.floodFill(borders, 255*np.ones(borders.shape[:-1]+(2,0)).astype(np.uint8), point, (0, 255, 255))


cv2.namedWindow("Borders", cv2.WINDOW_GUI_NORMAL)
cv2.setMouseCallback("Borders", on_click)

regions = {}
try:
    with open('regions.json', 'r') as file:
        regions = json.load(file)
except:
    pass

while True:
    cv2.imshow("Borders", borders)
    key = cv2.waitKey(20) 
    if key & 0xFF == ord('q'):
        break
    elif key & 0xFF == ord('c'):
        current_mask = borders[:, :, 1]
        blob = cv2.morphologyEx(borders[:, :, 1], cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
        contours, hierarchy = cv2.findContours(blob, cv2.RETR_CCOMP, cv2.CHAIN_APPROX_SIMPLE)
        polygons = np.zeros_like(borders)
        polygons = cv2.drawContours(polygons, contours, -1, (0,255,0), cv2.FILLED)
        cv2.imshow("Current Blob", polygons)
        if cv2.waitKey() & 0xFF == ord('y'):
            # Commit the region
            name = input("Name: ")
            borders[:, :, 2] = cv2.bitwise_and(borders[:, :, 2], cv2.bitwise_not(current_mask))
            regions[name] = {
                "contours": [contour.tolist() for contour in contours],
                "hierarchy": hierarchy.tolist()
            }
            with open('regions.json', 'w') as file:
                json.dump(regions, file)
        borders[:, :, 1] = 0
        cv2.destroyWindow("Current Blob")



