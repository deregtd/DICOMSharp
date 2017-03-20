class Point2D implements IPoint2D{
    constructor(public xPos: number, public yPos: number) {
    }

    addPoint(point: Point2D): Point2D {
        return new Point2D(this.xPos + point.xPos, this.yPos + point.yPos);
    }

    subtractPoint(point: Point2D): Point2D {
        return new Point2D(this.xPos - point.xPos, this.yPos - point.yPos);
    }

    multiplyBy(factor: number): Point2D {
        return new Point2D(this.xPos * factor, this.yPos * factor);
    }

    distanceFromOrigin(): number {
        return Math.sqrt(this.xPos * this.xPos + this.yPos * this.yPos);
    }

    normalized(): Point2D {
        const len = this.distanceFromOrigin();
        return new Point2D(this.xPos / len, this.yPos / len);
    }
}

export = Point2D;
