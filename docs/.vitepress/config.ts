import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'Termina',
  description: 'Reactive Terminal UI Framework for .NET',
  base: '/Termina/',

  head: [
    ['link', { rel: 'icon', href: '/Termina/termina-icon.png' }]
  ],

  themeConfig: {
    logo: '/termina-icon.png',

    nav: [
      { text: 'Guide', link: '/guide/' },
      { text: 'Tutorials', link: '/tutorials/' },
      { text: 'Layout', link: '/layout/' },
      { text: 'Components', link: '/components/' },
      { text: 'Styling', link: '/styling/' },
      { text: 'Concepts', link: '/concepts/' },
      { text: 'Advanced', link: '/advanced/' }
    ],

    sidebar: {
      '/guide/': [
        { text: 'Introduction', link: '/guide/' },
        { text: 'Getting Started', link: '/guide/getting-started' },
        { text: 'Installation', link: '/guide/installation' }
      ],
      '/tutorials/': [
        { text: 'Overview', link: '/tutorials/' },
        { text: 'Counter App', link: '/tutorials/counter-app' },
        { text: 'Todo List', link: '/tutorials/todo-list' },
        { text: 'Streaming Chat', link: '/tutorials/streaming-chat' }
      ],
      '/layout/': [
        { text: 'Layout System', link: '/layout/' },
        { text: 'Size Constraints', link: '/layout/size-constraints' },
        { text: 'Vertical & Horizontal', link: '/layout/vertical-horizontal' },
        { text: 'Nesting Layouts', link: '/layout/nesting' },
        { text: 'Responsive Design', link: '/layout/responsive' }
      ],
      '/components/': [
        { text: 'Component Library', link: '/components/' },
        { text: 'TextNode', link: '/components/text-node' },
        { text: 'PanelNode', link: '/components/panel-node' },
        { text: 'TextInputNode', link: '/components/text-input-node' },
        { text: 'SelectionListNode', link: '/components/selection-list-node' },
        { text: 'SpinnerNode', link: '/components/spinner-node' },
        { text: 'StreamingTextNode', link: '/components/streaming-text-node' },
        { text: 'ScrollableContainerNode', link: '/components/scrollable-container' },
        { text: 'StackLayout', link: '/components/stack-layout' },
        { text: 'ModalNode', link: '/components/modal-node' },
        { text: 'ReactiveLayoutNode', link: '/components/reactive-layout' },
        { text: 'ConditionalNode', link: '/components/conditional-node' },
        { text: 'EmptyNode', link: '/components/empty-node' },
        { text: 'DeferredNode', link: '/components/deferred-node' }
      ],
      '/styling/': [
        { text: 'Styling Guide', link: '/styling/' },
        { text: 'Colors', link: '/styling/colors' },
        { text: 'Text Formatting', link: '/styling/text-formatting' },
        { text: 'Borders', link: '/styling/borders' },
        { text: 'Theming', link: '/styling/theming' }
      ],
      '/concepts/': [
        { text: 'Core Concepts', link: '/concepts/' },
        { text: 'Architecture', link: '/concepts/architecture' },
        { text: 'Reactive Properties', link: '/concepts/reactive-properties' },
        { text: 'Observables & Rx', link: '/concepts/observables' },
        { text: 'Source Generators', link: '/concepts/source-generators' },
        { text: 'Routing', link: '/concepts/routing' },
        { text: 'Navigation', link: '/concepts/navigation' },
        { text: 'Input Handling', link: '/concepts/input-handling' },
        { text: 'Hosting & DI', link: '/concepts/hosting' }
      ],
      '/advanced/': [
        { text: 'Advanced Topics', link: '/advanced/' },
        { text: 'Testing', link: '/advanced/testing' },
        { text: 'Custom Components', link: '/advanced/custom-components' },
        { text: 'AOT Compilation', link: '/advanced/aot' },
        { text: 'Akka.NET Integration', link: '/advanced/akka-integration' }
      ],
      '/comparison/': [
        { text: 'Comparisons', link: '/comparison/' },
        { text: 'vs Spectre.Console', link: '/comparison/vs-spectre-console' },
        { text: 'vs Terminal.Gui', link: '/comparison/vs-terminal-gui' }
      ]
    },

    socialLinks: [
      { icon: 'github', link: 'https://github.com/Aaronontheweb/Termina' }
    ],

    editLink: {
      pattern: 'https://github.com/Aaronontheweb/Termina/edit/dev/docs/:path'
    },

    search: {
      provider: 'local'
    },

    footer: {
      message: 'Released under the Apache 2.0 License.',
      copyright: 'Copyright © 2025 <a href="https://aaronstannard.com/">Aaron Stannard</a>'
    }
  },

  markdown: {
    lineNumbers: true
  }
})
