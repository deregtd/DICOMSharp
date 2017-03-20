import React = require('react');

declare module 'svg-inline-react' {
    interface InlineSVGProps extends React.Props<InlineSVG> {
        src: string;
        element?: string;
        raw?: boolean;
    }

    class InlineSVG extends React.Component<InlineSVGProps, void> {}

    export = InlineSVG;
}
