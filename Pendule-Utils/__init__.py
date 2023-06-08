import pickle
import numpy as np
import os
import time



class Pendule:

    def __init__(self):
        self.xArray = []
        self.yArray = []
        self.minX = 0
        self.maxX = 0
        self.minY = 0
        self.maxY = 0
        self.circleXcenter = 0
        self.circleYcenter = 0

    def compute_min_max(self, num_iter):
        self.minX = min(self.xArray[-num_iter:])[0]
        self.maxX = max(self.xArray[-num_iter:])[0]
        self.minY = min(self.yArray[-num_iter:])[0]
        self.maxY = max(self.yArray[-num_iter:])[0]

    def configure(self, axis="x"):
        while len(self.xArray) < 100:
            time.sleep(1)

        if axis == "x":
            self.minX, self.maxX, _, _ = self.compute_min_max(100)
            self.circleXcenter = self.minX + (self.maxX - self.minX) / 2
        elif axis == "y":
            _, _, self.minY, self.maxY = self.compute_min_max(100)
            self.circleYcenter = self.minY + (self.maxY - self.minY) / 2
        self.save()
