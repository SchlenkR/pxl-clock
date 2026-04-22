import './styles.css';
import { render } from './render.ts';
import data from './data.json';
import type { IssuesData } from './types.ts';

const app = document.getElementById('app');
if (app) app.innerHTML = render(data as IssuesData);
