#!/usr/bin/python3

import socket
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.animation as animation
import re
import click
import time
import json

fig = plt.figure(figsize=(7, 7))
ax1 = fig.add_subplot(1, 1, 1)

print("Waiting for connection...")
serversocket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
host = '192.168.125.1'
port = 2090
serversocket.bind((host, port))
serversocket.listen(5)
clientsocket, address = serversocket.accept()
print(f"received connection from {address}")

xArray = []
yArray = []
circlex = 0
circley = 0
r = 0

minX = -1446
maxX = 1472
minY = -1584
maxY = 1630
circleXcenter = minX + (maxX - minX) / 2
circleYcenter = minY + (maxY - minY) / 2


# def thread_minmax():
#     while (True):
#         xmin, xmax, ymin, ymax = compute_min_max(100)
#         global circlex
#         circlex = xmin + (xmax - xmin) / 2
#         global circley
#         circley = ymin + (ymax - ymin) / 2
#         global r
#         r = np.sqrt((xmax - xmin) ** 2 + (ymax - ymin) ** 2) / 2
#         print("min max computed")
#         time.sleep(10)


@click.group()
def cli():
    pass


@cli.command()
@click.option('-t', default=False, help='Trace')
def plot(t):
    ani = animation.FuncAnimation(
        plt.gcf(), update_plot, interval=500, fargs=(t,))
    plt.tight_layout()
    plt.show()


@cli.command()
@click.option('-i', default=100, help='Number of iterations')
def minmax(i):
    for j in range(i):
        receive_data()
    xmin, xmax, ymin, ymax = compute_min_max(i)
    print(f"Xmin: {xmin}, Xmax: {xmax}, Ymin: {ymin}, Ymax: {ymax}")


@cli.command()
def mesure():
    data = []
    for i in range(5):
        position = receive_data()
        data.append({"x": position[0][0], "y": position[1][0]})
    with open('data/data.json', 'w') as outfile:
        json.dump(data, outfile)


def receive_data():
    message = clientsocket.recv(1024)
    message = message.decode('ascii')
    str = re.split(r'[,\n]', message)
    y = [float(str[0])]
    x = [float(str[1])]
    xArray.append(x)
    yArray.append(y)
    return x, y


def update_plot(i, trace=False):
    x, y = receive_data()
    if not trace:
        ax1.clear()
    if i % 20 == 0:
        xmin, xmax, ymin, ymax = compute_min_max(100)
        global circlex
        global circley
        global r
        circlex = xmin + (xmax - xmin) / 2
        circley = ymin + (ymax - ymin) / 2
        r = np.sqrt((xmax - xmin) ** 2 + (ymax - ymin) ** 2) / 2
        ax1.clear()
    ax1.axis([minX, maxX, minY, maxY])
    circleMinMax = plt.Circle((circley, circlex), r, color='b', fill=False)
    ax1.add_artist(circleMinMax)
    circleCenter = plt.Circle((circleXcenter, circleYcenter), 50, color='r', fill=False)
    if circleCenter.contains_point((y[0], x[0])):
        circleCenter.set_color('g')
        print("In the circle")
    ax1.add_artist(circleCenter)
    plt.gca().invert_yaxis()
    plt.plot(y, x, 'bo')


def compute_min_max(num_iter):
    return np.min(xArray[-1 - num_iter:]), np.max(xArray[-1 - num_iter:]), np.min(yArray[-1 - num_iter:]), np.max(yArray[-1 - num_iter:])


if __name__ == "__main__":
    cli()
