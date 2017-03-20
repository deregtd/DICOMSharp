import React = require('react');

// Taken from svg-inline-react and transcoded into typescript so it is minifiable

var _extends = (Object as any).assign || function (target: any) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i]; for (var key in source) { if (Object.prototype.hasOwnProperty.call(source, key)) { target[key] = source[key]; } } } return target; };

var DOMParser = typeof window !== 'undefined' && (window as any).DOMParser;

function isParserAvailable(src: any) {
    // kinda naive but meh, ain't gonna use full-blown parser for this
    return typeof DOMParser === 'function' && typeof src === 'string' && src.trim().substr(0, 4) === '<svg';
}

// parse SVG string using `DOMParser`
function parseFromSVGString(src: any) {
    var parser = new DOMParser();
    return parser.parseFromString(src, "image/svg+xml");
}

// Transform DOM prop/attr names applicable to `<svg>` element but react-limited
function switchSVGAttrToReactProp(propName: any) {
    switch (propName) {
        case 'class':
            return 'className';
        default:
            return propName;
    }
}

interface InlineSVGProps extends React.Props<any> {
    src: string;
    element?: string;
    raw?: boolean;
}

class InlineSVG extends React.Component<InlineSVGProps, {}> {
    private _serializeAttrs(map: any) {
        var ret = {};
        var prop: any;
        for (var i = 0; i < map.length; i++) {
            prop = switchSVGAttrToReactProp(map[i].name);
            ret[prop] = map[i].value;
        }
        return ret;
    }

    // get <svg /> element props
    private _extractSVGProps(src: any) {
        var map = parseFromSVGString(src).documentElement.attributes;
        return map.length > 0 ? this._serializeAttrs(map) : null;
    }

    // get content inside <svg> element.
    private _stripSVG(src: any) {
        return parseFromSVGString(src).documentElement.innerHTML;
    }

    render() {
        var svgProps = {};
        var src = this.props.src;
        var __html = src;
        var Element = this.props.element || 'i';

        if (this.props.children != null) {
            console.info('<InlineSVG />: `children` will be always ignored.');
        }

        if (this.props.raw === true) {
            if (isParserAvailable(src)) {
                Element = 'svg';
                svgProps = this._extractSVGProps(src);
                __html = this._stripSVG(src);
            } else {
                console && console.info('<InlineSVG />: `raw` prop works only when `window.DOMParser` exists.');
            }
        }

        return React.createElement(Element, _extends({}, svgProps, this.props, {
            src: null, children: null,
            dangerouslySetInnerHTML: { __html: __html }
        }));
    }
}

export = InlineSVG;
